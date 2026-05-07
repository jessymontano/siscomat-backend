# SISCOMAT Backend

## Requisitos para pruebas locales

- Instalar [Docker](https://www.docker.com/products/docker-desktop/)
- Clonar el [microservicio](https://github.com/VicAndMe/siscomat-microservicio) y [frontend](https://github.com/ebmv/siscomat-frontend)

## Para ejecutar todo el proyecto con Docker

### Paso 1: crear red compartida

Ejecuta este comando para crear la red (debe tener ese nombre exactamente)

```bash
docker network create siscomat_network
```

### Paso 2: levantar los tres proyectos

Ejecuta este comando en cada proyecto

```bash
docker compose up -d --build
```

### Paso 3: entrar a las interfaces

- http://localhost:5174/ para gestores
- http://localhost:5173/ para portal público
