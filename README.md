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

Design Principles
Separation of Concerns
Chatbot.cs: Console UI logic
ChatBotCore.cs: Conversation logic
KeywordResponder.cs: Knowledge base
SentimentDetector.cs: Tone adaptation
MemoryStore.cs: State management
Testability
Decoupled from file I/O where possible
Stateless response methods
Simple, predictable interfaces
Extensibility
Add new topics by extending KeywordResponder._responses
Add sentiment triggers to SentimentDetector._triggers
Extend memory storage by injecting a persistence adapter into MemoryStore

Improving Sentiment Detection
Current approach: Simple substring matching Recommendations:

Implement word-boundary checks to reduce false positives
Use regex patterns for more precise matching
Integrate an ML-based sentiment analysis library (e.g., SentiText)
Add stemming/normalization for variant word forms
Known Limitations
Keyword Matching: Substring-based matching may cause unintended collisions (e.g., "pass" matching in "password")
Sentiment Detection: Naive token-based approach; lacks contextual understanding
Response Variety: Fixed response lists may become repetitive over long conversations
Persistence: Console mode only persists user names, not conversation history
Language Support: English only
Future Enhancements
 Add conversation history logging
 Implement persistent conversation memory across sessions
 Integrate with a knowledge base API
 Add support for follow-up context resolution
 Implement proper NLP for better understanding
 Add multi-language support
 Create mobile app version
 Add quiz/assessment features
Requirements
.NET 6.0 or higher
C# 9.0+
System.IO namespace
System.Collections.Generic namespace
WPF framework (for UI mode)


SCREENSHOTS:

HOME PAGE - ENTER YOUR PAGE

<img width="1366" height="768" alt="image" src="https://github.com/user-attachments/assets/28af7aed-e4ce-467f-8d56-d4abc5eca9cd" />

TOPIC MENU:

<img width="1366" height="768" alt="image" src="https://github.com/user-attachments/assets/1bf275ce-da3e-43c6-9fbd-93fdad69cc63" />


