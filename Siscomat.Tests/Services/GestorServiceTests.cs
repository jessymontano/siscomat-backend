using FluentAssertions;
using Moq;
using Siscomat.Core.Entities;
using Siscomat.Core.Interfaces;
using Siscomat.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Siscomat.Tests.Services
{
    [TestFixture]
    public class GestorServiceTests
    {
        private Mock<IGestorRepository> _gestorRepositoryMock;
        private GestorService _service;

        [SetUp]
        public void SetUp()
        {
            _gestorRepositoryMock = new Mock<IGestorRepository>();
            _service = new GestorService(_gestorRepositoryMock.Object);
        }

        [Test]
        public async Task CreateGestor_HashesPasswordBeforeSaving()
        {
            // Arrange
            var gestor = new Core.Entities.Gestor
            {
                Id = 1,
                Nombre = "Karl",
                Correo = "prueba@uson.mx",
                PasswordHash = "123456",
                EsAdmin = false
            };

            Gestor savedGestor = null;

            _gestorRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Gestor>()))
                .Callback<Gestor>(g => savedGestor = g)
                .Returns(Task.CompletedTask);

            // Act
            await _service.AddAsync(gestor);

            // Assert
            savedGestor.Should().NotBeNull();
            savedGestor.PasswordHash.Should().NotBe("123456");
            savedGestor.PasswordHash.Should().StartWith("$2");
            savedGestor.PasswordHash.Length.Should().Be(60);
        }

        [Test]
        public async Task UpdateGestor_OnlyChangeName_KeepsOtherFieldsIntact()
        {
            int id = 1;
            var updatedGestor = new Gestor
            {
                Nombre = "Karl2",
                Apellido1 = null,
                Correo = null,
                PasswordHash = null,
            };

            var originalGestor = new Gestor
            {
                Id = id,
                Nombre = "Karl",
                Apellido1 = "Marx",
                Correo = "prueba@uson.mx",
                PasswordHash = "123456",
                EsAdmin = false
            };

            _gestorRepositoryMock.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(originalGestor);

            Gestor savedGestor = null;
            _gestorRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Gestor>()))
                .Callback<Gestor>(g => savedGestor = g)
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAsync(id, updatedGestor);

            // Assert
            savedGestor.Should().NotBeNull();
            savedGestor.Nombre.Should().Be("Karl2");
            savedGestor.Apellido1.Should().Be("Marx");
            savedGestor.Correo.Should().Be("prueba@uson.mx");
            savedGestor.PasswordHash.Should().Be("123456");
            savedGestor.EsAdmin.Should().BeFalse();
            savedGestor.Id.Should().Be(id);
        }

        [Test]
        public async Task UpdateAsync_ChangePassword_HashesNewPassword()
        {
            // Arrange
            int id = 1;
            var originalGestor = new Gestor
            {
                Id = id,
                Nombre = "Karl",
                PasswordHash = "oldpassword"
            };

            var updatedGestor = new Gestor
            {
                Nombre = "Karl",
                PasswordHash = "newpassword"
            };

            _gestorRepositoryMock.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(originalGestor);

            // Act
            await _service.UpdateAsync(id, updatedGestor);

            // Assert
            originalGestor.PasswordHash.Should().NotBe("oldpassword");
            originalGestor.PasswordHash.Should().NotBe("newpassword");
            originalGestor.PasswordHash.Should().StartWith("$2");

            _gestorRepositoryMock.Verify(r => r.UpdateAsync(originalGestor), Times.Once);
        }
    }
}
