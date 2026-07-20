<div align="center">

# Inkboard

[About](#about) · [Preview](#preview) · [Schema](#schema) · [Setup](#setup) · [Docs](#docs)

Simple real-time canvas for shared drawing.

[![React](https://img.shields.io/badge/React-20232A?style=flat&logo=react&logoColor=61DAFB)](https://react.dev/)
[![Vite](https://img.shields.io/badge/Vite-646CFF?style=flat&logo=vite&logoColor=FFFFFF)](https://vitejs.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=flat&logo=typescript&logoColor=FFFFFF)](https://www.typescriptlang.org/)
[![.NET 10](https://img.shields.io/badge/.NET-512BD4?style=flat&logo=dotnet&logoColor=FFFFFF)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-512BD4?style=flat&logo=dotnet&logoColor=FFFFFF)](https://learn.microsoft.com/aspnet/core/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-336791?style=flat&logo=postgresql&logoColor=FFFFFF)](https://www.postgresql.org/)
[![SignalR](https://img.shields.io/badge/SignalR-1E88E5?style=flat&logo=microsoft&logoColor=FFFFFF)](https://learn.microsoft.com/aspnet/core/signalr/introduction)

</div>

## About

Inkboard is a shared drawing app made for quick live collaboration. It is inspired by simple sketching tools like MS Paint and by party-style group rooms, so it stays easy to use while still feeling social.

## What It Does

- Lets multiple people draw on the same canvas at the same time.
- Shows updates in real time.
- Keeps drawing simple, fast, and easy to follow.

## Main Features

- Shared live canvas for group drawing.
- Simple room-based sessions for friends or teammates.
- Fast feedback while you draw.
- Clean browser-based experience.

## Preview

### Landing Page

<p align="center">
  <img src="./assets/landing.png" alt="Inkboard landing page" width="100%">
</p>

### Doodles Page

<p align="center">
  <img src="./assets/doodles.png" alt="Inkboard doodles page" width="100%">
</p>

## Schema

This diagram shows the database schema used by the app.

<p align="center">
  <img src="./assets/er_diagram.png" alt="Inkboard database schema" width="100%">
</p>

## Setup

### Client

1. Go to `client/`.
2. Run `npm install`.
3. Run `npm run dev`.

### Server

1. Go to `server/`.
2. Run `dotnet restore`.
3. Run `dotnet run --project Inkboard.API`.

## Docs

If you want more detail, start with [ARCHITECTURE.md](/home/jymeng18/Projects/Inkboard/docs/ARCHITECTURE.md).

## Notes

The project is still growing, so small changes may happen over time.