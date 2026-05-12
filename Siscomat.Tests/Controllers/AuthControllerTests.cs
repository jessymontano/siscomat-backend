using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.AspNetCore.Authentication;
using Moq;
using Siscomat.Api.Controllers;
using Siscomat.Core.DTOs;
using Siscomat.Core.Entities;
using Siscomat.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Claims;

namespace Siscomat.Tests.Controllers
{
    [TestFixture]
    public class AuthControllerTests
    {
        private Mock<IAuthService> _authServiceMock;
        private Mock<IAuthenticationService> _authenticationServiceMock;
        private AuthController _controller;

        [SetUp]
        public void Setup()
        {
            _authServiceMock = new Mock<IAuthService>();
            _controller = new AuthController(_authServiceMock.Object);

            _authenticationServiceMock = new Mock<IAuthenticationService>();

            _authenticationServiceMock
                .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            _authenticationServiceMock
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(_authenticationServiceMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = serviceProviderMock.Object;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Test]
        public async Task Login_CorrectCredentials_ReturnsOkAndCookie()
        {
            // Arrange
            var loginDto = new LoginDTO
            {
                Correo = "gestor@unison.mx",
                Password = "password123"
            };

            var gestorFalso = new Gestor
            {
                Id = 1,
                Nombre = "Gestor de Prueba",
                Correo = "Karl"
            };

            _authServiceMock
                .Setup(s => s.LoginAsync(loginDto))
                .ReturnsAsync(gestorFalso);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            okResult.Value.Should().BeEquivalentTo(new { message = "Inicio de sesión exitoso" });

            _authenticationServiceMock.Verify(a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
                Times.Once);
        }

        [Test]
        public async Task Login_IncorrectCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDTO
            {
                Correo = "gestor@unison.mx",
                Password = "wrongpassword"
            };

            _authServiceMock
                .Setup(s => s.LoginAsync(loginDto))
                .ReturnsAsync((Gestor)null);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
            unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Correo o contraseña incorrectos" });
        }

        [Test]
        public async Task Login_EmptyFields_RejectsConnection()
        {
            // Arrange
            var loginDto = new LoginDTO
            {
                Correo = "",
                Password = ""
            };
            // Act
            var result = await _controller.Login(loginDto);
            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            badRequestResult.Value.Should().BeEquivalentTo(new { message = "Correo y contraseña son requeridos" });
        }

        [Test]
        public async Task Login_InvalidEmail_RejectsConnection()
        {
            // Arrange
            var loginDto = new LoginDTO
            {
                Correo = "invalid-email",
                Password = "password123"
            };
            // Act
            var result = await _controller.Login(loginDto);
            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            badRequestResult.Value.Should().BeEquivalentTo(new { message = "Formato de correo electrónico inválido" });
        }

        [Test]
        public async Task Logout_ActiveSession_DestroysSession()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "Gestor de Prueba"),
                new Claim(ClaimTypes.Email, "gestor@unison.mx")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;

            // Act
            var result = await _controller.Logout();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            okResult.Value.Should().BeEquivalentTo(new { message = "Sesión cerrada exitosamente" });
        }

        [Test]
        public async Task Logout_NoActiveSession_RejectsConnection()
        {
            // Arrange
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = await _controller.Logout();

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
            unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "No hay sesión activa." });
        }

        [Test]
        public void Logged_ActiveSession_ReturnsOk()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "Gestor de Prueba"),
                new Claim(ClaimTypes.Email, "gestor@unison.mx")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;

            // Act
            var result = _controller.Logged();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            okResult.Value.Should().BeEquivalentTo($"Bienvenido, Gestor de Prueba");
        }

        [Test]
        public void Logged_NoActiveSession_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = _controller.Logged();

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
            unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "No hay sesión activa." });
        }
    }
}