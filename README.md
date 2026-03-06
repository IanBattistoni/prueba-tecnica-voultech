<p align="center">
  <img src="https://voultech.com/wp-content/uploads/2024/05/logo-voultech-rectangular-12.png" width="400">
</p>

# Prueba Tecnica API

API REST desarrollada en .NET utilizando PostgreSQL.

# Instalación y ejecución

## 1. Clonar repositorio
```bash
git clone https://github.com/IanBattistoni/exdev-website.git
```

## 2. Instalar dependencias
```bash
dotnet restore
```

## 3. Correr la API
```bash
dotnet run
```


# Despliegue

### Producción (Azure)

https://prueba-tecnica-fxakc9fwg5c0c7f3.chilecentral-01.azurewebsites.net/api

### Local

http://localhost:5225/api


# Endpoints

## Órdenes

| Método | Endpoint | Descripción | Ejemplo Body |
|------|------|------|------|
| GET | `/ordenes` | Lista todas las órdenes de compra | — |
| GET | `/ordenes/{id}` | Obtiene una orden de compra específica | — |
| POST | `/ordenes` | Crea una nueva orden de compra con productos | `{ "clienteId": 1, "clienteNombre": "Juan", "productos": [1,2,3] }` |
| PUT | `/ordenes/{id}` | Edita una orden de compra existente | `{ "clienteId": 1, "clienteNombre": "Juan", "productos": [1,1,3] }` |
| DELETE | `/ordenes/{id}` | Elimina una orden de compra y sus asociaciones | — |

---

## Productos

| Método | Endpoint | Descripción | Ejemplo Body |
|------|------|------|------|
| GET | `/productos` | Lista todos los productos | — |
| POST | `/productos` | Crea un nuevo producto | `{ "nombreProducto": "Teclado", "precioProducto": 19990 }` |

---

## Clientes

| Método | Endpoint | Descripción | Ejemplo Body |
|------|------|------|------|
| GET | `/users` | Lista todos los clientes | — |
| GET | `/users/{id}` | Obtiene un cliente por ID | — |
| POST | `/users` | Crea un nuevo cliente | `{ "name": "Juan" }` |


## Requisitos

- .NET SDK 10