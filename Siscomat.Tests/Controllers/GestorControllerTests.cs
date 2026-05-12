using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Siscomat.Api.Controllers;
using Siscomat.Core.Entities;
using Siscomat.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Siscomat.Tests.Controllers
{
    [TestFixture]
    public class GestorControllerTests
    {
        private Mock<IGestorService> _gestorServiceMock;
        private GestorController _controller;

        [SetUp]
        public void SetUp()
        {
            _gestorServiceMock = new Mock<IGestorService>();
            _controller = new GestorController(_gestorServiceMock.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Test]
        public async Task GetGestores_WithAdminUser_ReturnsList()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Gestor de Prueba"),
                new Claim(ClaimTypes.Email, "gestor@unison.mx"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

            var gestoresFalsos = new List<Gestor>
            {
                new Gestor { Id = 1, Nombre = "Karl", Apellido1 = "Marx",Correo = "gestor@unison.mx", EsAdmin = false },
                new Gestor { Id = 2, Nombre = "Friedrich", Apellido1 = "Nietzsche", Correo = "admin@unison.mx", EsAdmin = true }
            };

            var expectedList = new[]
            {
                new {
                    id = 1,
                    nombre = "Karl",
                    apellido1 = "Marx",
                    apellido2 = (string)null,
                    correo = "gestor@unison.mx",
                    esAdmin = false
                },
                new {
                    id = 2,
                    nombre = "Friedrich",
                    apellido1 = "Nietzsche",
                    apellido2 = (string)null,
                    correo = "admin@unison.mx",
                    esAdmin = true
                }
            };

            _gestorServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(gestoresFalsos);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            okResult.Value.Should().BeEquivalentTo(expectedList);
        }

        [Test]
        public async Task GetGestores_WithNoAdminUser_ReturnsForbidden()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Gestor de Prueba"),
                new Claim(ClaimTypes.Email, "gestor@unison.mx"),
                new Claim(ClaimTypes.Role, "Gestor")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var forbiddenResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
            forbiddenResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            forbiddenResult.Value.Should().BeEquivalentTo(new { error = "No tienes permisos para realizar esta acción." });
        }

        [Test]
        public async Task GetGestores_NoUser_ReturnsUnauthorized()
        {
            //Arrange
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            // Act
            var result = await _controller.GetAll();
            // Assert
            var unauthorizedResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
            unauthorizedResult.Value.Should().BeEquivalentTo(new { error = "No hay sesión activa." });
        }

        [Test]
        public async Task CreateGestor_ValidDataAndAdmin_RegistersAndReturnsObject()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Gestor de Prueba"),
                new Claim(ClaimTypes.Email, "admin@admin.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
            var newGestor = new Gestor { Nombre = "Karl", Apellido1 = "Marx", Correo = "prueba@uson.mx", PasswordHash = "123456", EsAdmin = false };
            var createdGestor = new {
                id = 1,
                nombre = "Karl",
                apellido_1 = "Marx",
                apellido2 = (string)null,
                correo = "prueba@uson.mx",
                esAdmin = false 
            };

            _gestorServiceMock.Setup(s => s.AddAsync(It.IsAny<Gestor>()))
                .Callback<Gestor>(g =>
                    {
                        typeof(Gestor).GetProperty("Id").SetValue(g, 1);
                    })
                .Returns(Task.CompletedTask);

            _gestorServiceMock.Setup(s => s.GetByCorreoAsync(newGestor.Correo))
                  .ReturnsAsync((Gestor)null);

            // Act
            var result = await _controller.Create(newGestor);

            // Assert
            var okResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status201Created);
            okResult.Value.Should().BeEquivalentTo(createdGestor);
        }

        [Test]
        public async Task CreateGestor_EmptyData_ReturnsBadRequest()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Gestor de Prueba"),
                new Claim(ClaimTypes.Email, "admin@admin.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
            var newGestor = new Gestor { Nombre = "Karl", Apellido1 = "", Correo = "", PasswordHash = "123456", EsAdmin = false };

            // Act
            var result = await _controller.Create(newGestor);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            badRequestResult.Value.Should().BeEquivalentTo(new { error = "Campos faltantes." });
        }

        [Test]
        public async Task CreateGestor_ExistingEmail_ReturnsConflict()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Gestor de Prueba"),
                new Claim(ClaimTypes.Email, "admin@admin.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
            var existingGestor = new Gestor { Id = 1, Nombre = "Karl", Apellido1 = "Marx", Correo = "prueba@uson.mx", PasswordHash = "123456", EsAdmin = false };
            var newGestor = new Gestor { Nombre = "Karl", Apellido1 = "2", Correo = "prueba@uson.mx", PasswordHash = "123456", EsAdmin = false };

            _gestorServiceMock.Setup(s => s.GetByCorreoAsync(newGestor.Correo))
                .ReturnsAsync(existingGestor);

            // Act
            var result = await _controller.Create(newGestor);

            // Assert
            var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflictResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
            conflictResult.Value.Should().BeEquivalentTo(new { error = "Ya existe un gestor registrado con ese correo." });
        }

        [Test]
        public async Task DeleteGestor_ValidId_ReturnsOk()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Gestor de Prueba"),
                new Claim(ClaimTypes.Email, "admin@admin.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

            int id = 1;
            var existingGestor = new Gestor { Id = id, Nombre = "Karl", Correo = "prueba@uson.mx", EsAdmin = false };

            _gestorServiceMock.Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync(existingGestor);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            okResult.Value.Should().BeEquivalentTo(new { message = "Gestor eliminado correctamente." });

            _gestorServiceMock.Verify(s => s.DeleteAsync(id), Times.Once);
        }

        [Test]
        public async Task DeleteGestor_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Gestor de Prueba"),
                new Claim(ClaimTypes.Email, "admin@admin.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

            int id = 67;

            _gestorServiceMock.Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync((Gestor)null);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
            notFoundResult.Value.Should().BeEquivalentTo(new { error = "No existe un gestor con ese id." });

            _gestorServiceMock.Verify(s => s.DeleteAsync(It.IsAny<int>()), Times.Never);
        }
    }
}