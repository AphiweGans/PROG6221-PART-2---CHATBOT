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
- **Usage:**

2. ChatBotCore.cs (UI Implementation)
Lightweight, UI-focused chatbot implementation used by the WPF frontend.

Key Features:

Name capture on first interaction
Stateful processing with greeting and input handling
Session-based memory tracking
Decoupled from persistence for testability


3. KeywordResponder.cs
Maps topic keywords to randomized response lists.

Supported Topics:

Phishing
Password management
Malware
Encryption
Privacy
Ransomware
Scams
Two-factor authentication
DDoS attacks
Safe browsing
Key Methods:

TryGetResponse(string input) - Returns matching topic and random response
GetRandomResponseForKey(string key) - Retrieves additional responses for a topic
GetAllKeywords() - Lists all available topics


4. SentimentDetector.cs
Naive token-based sentiment analysis for adaptive chatbot tone.

Supported Sentiments:

Worried: Concerned/scared language triggers supportive responses
Curious: Inquisitive language receives educational openers
Frustrated: Overwhelmed language gets simplified explanations
Confident: Knowledgeable language receives advanced tips
Happy: Positive language gets encouraging follow-ups
Trigger Examples:

Worried: "worried", "scared", "unsafe"
Curious: "curious", "wonder", "how does"
Frustrated: "frustrated", "don't understand", "overwhelmed"
Confident: "I know", "I understand", "got it"
Happy: "great", "thanks", "awesome"


5. MemoryStore.cs
In-memory key-value store for session state persistence.

Stored Values:

UserName - User's display name
FavouriteTopic - User's preferred cybersecurity topic
Methods:

Store(string key, string? value) - Save or remove a value
Get(string key) - Retrieve a value
GetPersonalisedOpener() - Generate context-aware greetings
