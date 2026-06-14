# AI Chatbot App

A modern full-stack AI chatbot application built with **ASP.NET Core**, **Vue.js**, and **PostgreSQL**.
The app provides a clean custom AI chat interface with conversation history, image upload analysis, markdown rendering, code block copy support, multi-provider AI fallback, and limited AI image generation.

## Live Demo

[View Live Project](https://arghochatbot.vercel.app/)

## Features

* AI-powered text chat
* Conversation-based chat history
* Create, rename, delete, and clear conversations
* Image upload and image analysis support
* Limited AI image generation using prompt commands
* Markdown response rendering
* Safe HTML sanitization with DOMPurify
* Code block rendering with copy button
* Multi-provider AI fallback system
* Provider cooldown and retry handling
* PostgreSQL database integration
* ASP.NET Core REST API backend
* Vue 3 + Vite frontend
* Tailwind CSS modern dark UI
* Docker support for backend deployment

## Tech Stack

### Frontend

* Vue.js 3
* Vite
* Tailwind CSS
* JavaScript
* Markdown-it
* DOMPurify
* Font Awesome

### Backend

* ASP.NET Core
* C#
* Entity Framework Core
* PostgreSQL
* REST API
* HttpClient
* Session support
* Docker

### AI Services

* Gemini API
* Cloudflare Workers AI
* Multi-provider fallback architecture

## Project Structure

```txt
aichatbotapp/
├── Controllers/
│   ├── ApiChatController.cs
│   ├── ChatController.cs
│   └── HomeController.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── ChatMessage.cs
│   ├── Conversation.cs
│   ├── GeneratedImage.cs
│   └── UploadedImage.cs
├── Services/
│   ├── AiProviderRouterService.cs
│   ├── AiVisionRouterService.cs
│   ├── CloudflareImageGenerationService.cs
│   └── Providers/
├── ViewModels/
├── Views/
├── wwwroot/
├── frontend/
│   ├── src/
│   │   ├── App.vue
│   │   ├── main.js
│   │   └── style.css
│   ├── package.json
│   └── vite.config.js
├── Dockerfile
├── Program.cs
└── AiChatbotApp.csproj
```

## Core Functionalities

### Text Chat

Users can send messages and receive AI-generated responses through the chat interface. The backend stores conversations and previous messages, allowing the assistant to respond with relevant context.

### Image Analysis

Users can upload images along with a prompt. The backend saves the uploaded image and sends it to the configured vision provider for analysis.

### AI Image Generation

The app supports limited image generation through prompt commands such as:

```txt
/image your prompt here
/generate-image your prompt here
generate image: your prompt here
```

Generated images are saved and displayed inside the chat response.

### Conversation Management

Users can:

* Create new conversations
* View previous conversations
* Rename conversations
* Delete conversations
* Clear chat history

### Multi-Provider AI Fallback

The backend uses a provider router system. If one AI provider fails, reaches quota, or becomes unavailable, the system can move to another configured provider.

## API Overview

### Conversation APIs

```txt
GET    /api/chat/conversations
POST   /api/chat/conversations
DELETE /api/chat/conversations/{id}
PATCH  /api/chat/conversations/{id}/title
```

### Chat APIs

```txt
GET    /api/chat/history?conversationId={id}
POST   /api/chat/send
DELETE /api/chat/clear
GET    /api/chat/providers/status
```

### Health Check

```txt
GET /health
```

## Local Setup

### Backend Setup

```bash
dotnet restore
dotnet build
dotnet run
```

Backend default local port:

```txt
http://localhost:5077
```

### Frontend Setup

```bash
cd frontend
npm install
npm run dev
```

Frontend default local port:

```txt
http://localhost:5173
```

## Environment Configuration

Add the required API keys and database connection string in `appsettings.json` or environment variables.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your PostgreSQL connection string"
  },
  "Gemini": {
    "ApiKey": "Your Gemini API key"
  },
  "Cloudflare": {
    "AccountId": "Your Cloudflare account id",
    "ApiToken": "Your Cloudflare API token",
    "ImageModel": "@cf/stabilityai/stable-diffusion-xl-base-1.0"
  }
}
```

## Docker

Build and run the backend using Docker:

```bash
docker build -t aichatbotapp .
docker run -p 5077:5077 aichatbotapp
```

## Project Purpose

This project was created to explore a professional full-stack AI assistant architecture using ASP.NET Core as the backend API and Vue.js as the frontend. It demonstrates AI provider integration, chat memory, image analysis, image generation, API design, database persistence, and modern responsive UI design.

## Author

Developed by **Argho Chakma**.
