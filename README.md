```text
      ___           ___                         ___                         ___                         ___     
     /\  \         /\  \                       /\  \                       /\  \         _____         /\__\    
    /::\  \        \:\  \         ___         /::\  \         ___          \:\  \       /::\  \       /:/ _/_   
   /:/\:\  \        \:\  \       /\__\       /:/\:\  \       /\__\          \:\  \     /:/\:\  \     /:/ /\__\  
  /:/ /::\  \   ___  \:\  \     /:/  /      /:/  \:\  \     /:/  /      ___  \:\  \   /:/ /::\__\   /:/ /:/ _/_ 
 /:/_/:/\:\__\ /\  \  \:\__\   /:/__/      /:/__/ \:\__\   /:/__/      /\  \  \:\__\ /:/_/:/\:|__| /:/_/:/ /\__\
 \:\/:/  \/__/ \:\  \ /:/  /  /::\  \      \:\  \ /:/  /  /::\  \      \:\  \ /:/  / \:\/:/ /:/  / \:\/:/ /:/  / 
  \::/__/       \:\  /:/  /  /:/\:\  \      \:\  /:/  /  /:/\:\  \      \:\  /:/  /   \::/_/:/  /   \::/_/:/  /  
   \:\  \        \:\/:/  /   \/__\:\  \      \:\/:/  /   \/__\:\  \      \:\/:/  /     \:\/:/  /     \:\/:/  /  
    \:\__\        \::/  /         \:\__\      \::/  ǀ         \:\__\      \::/  /       \::/  /       \::/  /   
     \/__/         \/__/           \/__/       \/__/           \/__/       \/__/         \/__/         \/__/    

                                                                                            { by Johnny Romero; }
```

# 🎥 AutoTube.AI.Console

**AutoTube.AI.Console** es un software en desarrollo que combina inteligencia artificial y .NET para **generar videos de manera totalmente automática**.  
La idea es construir un sistema que, consumiendo distintos servicios de IA vía API, pueda producir contenido estilo *slideshow* con:

- Guiones generados automáticamente.  
- Narraciones con voces sintéticas.  
- Miniaturas para acompañar el video.  
- Edición básica y transiciones con movimiento.  

Este repositorio es parte de una serie de contenidos donde muestro, paso a paso, cómo se diseña y construye un software desde cero, uniendo **arquitectura de software, desarrollo en .NET y servicios de IA**.

---

## 🚀 Objetivos del Proyecto

- Mostrar cómo diseñar un sistema modular y escalable en .NET.  
- Resolver los retos de integrar servicios de IA para:  
  - Generar guiones.  
  - Crear narraciones de audio.  
  - Generar miniaturas.  
  - Automatizar edición básica de video.  
- Explorar el impacto de la IA en la creación de contenido para **YouTube, redes sociales y marketing digital**.

---

## 📂 Estructura del Proyecto

```plaintext
AutoTube.AI.Console
│
├── Dependencies/        # Dependencias externas
├── Services/            # Lógica de conexión con APIs de IA
│   └── OpenAIService.cs # Servicio para interactuar con OpenAI
│
├── Program.cs           # Punto de entrada principal
