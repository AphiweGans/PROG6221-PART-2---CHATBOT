# PROG6221-PART-2---CHATBOT

# CyberAware Chatbot

A comprehensive cybersecurity awareness chatbot built with C# featuring both console and WPF UI implementations. The chatbot educates users on cybersecurity topics including phishing, passwords, malware, encryption, privacy, ransomware, scams, two-factor authentication, DDoS attacks, and safe browsing practices.

## Features

- **Dual Interface Support**
  - Console-based chatbot for traditional CLI usage
  - WPF (Windows Presentation Foundation) UI for modern desktop experience
  
- **Intelligent Responses**
  - Keyword-based topic detection
  - Sentiment analysis for adaptive tone
  - Multiple randomized responses per topic to prevent repetitive interactions
  
- **Session Persistence**
  - User tracking (console mode)
  - In-memory session personalization (UI mode)
  - Favorite topic tracking
  
- **Interactive Features**
  - "Tell me more" follow-up support
  - Multiple question handling in single input
  - Typed response animation (console mode)
  - Dynamic sentiment-based openers

## Architecture

### Core Components

#### 1. **Chatbot.cs** (Console Implementation)
The original console-based chatbot preserved for compatibility and reference.

**Key Features:**
- Interactive console loop with user prompts
- User persistence to `assets/users.txt`
- Returning user detection with personalized greetings
- Support for multiple questions in a single input
- Typed response simulation with character delays

**Usage:**
```csharp
var chatbot = new Chatbot("YourName");
      chatbot.Start();

      2. ChatBotCore.cs (UI Implementation)
Lightweight, UI-focused chatbot implementation used by the WPF frontend.

Key Features:

Name capture on first interaction
Stateful processing with greeting and input handling
Session-based memory tracking
Decoupled from persistence for testability
Usage:

C#
var core = new ChatBotCore();
string greeting = core.GetGreeting();
string response = core.ProcessInput(userInput);
var memory = core.GetMemory();
