<div align="center">

# Inkboard

Simple real-time canvas for shared drawing.

[![React](https://img.shields.io/badge/React-FF6B6B?style=for-the-badge&logo=react&logoColor=FFFFFF)](https://react.dev/)
[![Vite](https://img.shields.io/badge/Vite-FFD23F?style=for-the-badge&logo=vite&logoColor=1F1F1F)](https://vitejs.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-4F8DF7?style=for-the-badge&logo=typescript&logoColor=FFFFFF)](https://www.typescriptlang.org/)
[![.NET 10](https://img.shields.io/badge/.NET-8B5CF6?style=for-the-badge&logo=dotnet&logoColor=FFFFFF)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-25C2A0?style=for-the-badge&logo=dotnet&logoColor=FFFFFF)](https://learn.microsoft.com/aspnet/core/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-3E7BC0?style=for-the-badge&logo=postgresql&logoColor=FFFFFF)](https://www.postgresql.org/)
[![SignalR](https://img.shields.io/badge/SignalR-FF4D9D?style=for-the-badge&logo=microsoft&logoColor=FFFFFF)](https://learn.microsoft.com/aspnet/core/signalr/introduction)


[About](#about) · [Preview](#preview) · [Schema](#schema) · [Setup](#setup) · [Docs](#docs)

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

### Demo Video

<p align="center">
  <video controls autoplay muted loop playsinline width="100%" poster="./assets/landing.png">
    <source src="./assets/inkboard_demo.mp4" type="video/mp4">
    Your browser does not support the video tag.
  </video>
</p>

## Schema

This diagram shows the database schema used by the app.

<p align="center">
  <img src="./assets/er_diagram.png" alt="Inkboard database schema" width="100%">
</p>

## Setup

### Client

1. Go to `client/`.
2. Run `pnpm install`.
3. Run `pnpm run dev`.

### Server

1. Go to `server/`.
2. Run `dotnet restore`.
3. Run `dotnet run --project Inkboard.API`.

## Docs

If you want more detail, start with [ARCHITECTURE.md](/home/jymeng18/Projects/Inkboard/docs/ARCHITECTURE.md).

## Notes

The project is still growing, so small changes may happen over time.