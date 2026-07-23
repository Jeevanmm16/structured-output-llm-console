# 🎫 Structured Output LLM Console

A C# .NET 8 console application that demonstrates how to reliably extract **structured JSON data from a local Large Language Model (LLM)** using [Ollama](https://ollama.com).

The project focuses on the fundamentals of reliable LLM output handling:

* Structured JSON output
* Reliable JSON extraction
* JSON parsing
* Model-based schema/data validation
* Retry handling for invalid LLM responses
* Deterministic unit testing
* Separation of parsing and validation concerns

The application takes free-form support ticket text and converts it into a structured `SupportTicket` object containing:

* `category`
* `priority`
* `summary`

---

## 📋 Table of Contents

* [Overview](#-overview)
* [What This Project Demonstrates](#-what-this-project-demonstrates)
* [Example](#-example)
* [How It Works](#-how-it-works)
* [Architecture](#-architecture)
* [Project Structure](#-project-structure)
* [Prerequisites](#-prerequisites)
* [Ollama Setup](#-ollama-setup)
* [Running the Application](#-running-the-application)
* [Running the Tests](#-running-the-tests)
* [Test Suite](#-test-suite)
* [Retry Mechanism](#-retry-mechanism)
* [Key Design Decisions](#-key-design-decisions)
* [Concepts Covered](#-concepts-covered)
* [Scope](#-scope)
* [Future Extensions](#-future-extensions)

---

## 🧠 Overview

LLMs are powerful at understanding natural language, but their output is probabilistic. Even when instructed to return JSON, a model may produce:

* Valid JSON
* Malformed JSON
* Missing required fields
* Incorrect data types
* Empty responses
* Markdown-wrapped JSON
* Additional explanatory text

This project demonstrates a simple but reliable pipeline for handling these situations.

The application does not blindly trust the LLM response.

Instead, every response goes through:

```text
LLM Response
     │
     ▼
JSON Parsing
     │
     ▼
Data / Schema Validation
     │
     ▼
Valid?
  ┌──┴──┐
 YES    NO
  │      │
  │      ▼
  │   Retry with
  │   error feedback
  │      │
  │      ▼
  │   Max 3 attempts
  │
  ▼
Structured SupportTicket
```

---

# 🎯 What This Project Demonstrates

## 1. Structured Output

By default, an LLM may respond with natural language.

For example:

```text
Your payment appears to have failed multiple times. Please try again or contact your bank.
```

For an application, however, it is often more useful to receive structured data:

```json
{
  "category": "Payment",
  "priority": "High",
  "summary": "Customer's credit card payment failed three times and requires urgent assistance."
}
```

The application can then deserialize this JSON into a strongly typed C# object:

```csharp
SupportTicket
{
    Category = "Payment",
    Priority = "High",
    Summary = "Customer's credit card payment failed three times and requires urgent assistance."
}
```

---

## 2. Reliable JSON Extraction

The application handles multiple possible LLM response failures.

| Failure Scenario | Example                     | Handling                      |
| ---------------- | --------------------------- | ----------------------------- |
| Empty response   | `""`                        | Parsing failure               |
| Malformed JSON   | Missing closing `}`         | Parsing failure               |
| Missing field    | No `summary`                | Parsed, then validation fails |
| Wrong data type  | `"priority": 123`           | Parsing failure               |
| Valid JSON       | All expected fields present | Validation succeeds           |

### Important Concept

**Valid JSON syntax does not necessarily mean valid application data.**

For example:

```json
{
  "category": "Payment",
  "priority": "High"
}
```

This is valid JSON.

However, it does not contain the required `summary` field.

The JSON parser may successfully deserialize it, but the validation layer must reject it.

This project intentionally separates:

```text
JSON Parsing
    ≠
Data Validation
```

---

## 3. Model-Based Schema and Data Validation

The expected structure is represented by the `SupportTicket` model:

```csharp
public class SupportTicket
{
    public string Category { get; set; }
    public string Priority { get; set; }
    public string Summary { get; set; }
}
```

The `TicketValidator` then verifies that the required fields contain valid values.

The validation process checks:

* `Category` is not empty
* `Priority` is not empty
* `Summary` is not empty

This project uses **model-based validation** rather than an external JSON Schema library.

The purpose is to demonstrate the fundamental concept:

```text
LLM Output
    ↓
Can it be parsed?
    ↓
Does it contain valid required data?
    ↓
Can the application safely use it?
```

---

## 4. Retry Handling

If the LLM produces invalid output, the application does not immediately fail.

Instead, it retries the request.

The retry prompt includes:

* Original user input
* The problem detected in the previous response
* The expected output structure

The application allows a maximum of **3 attempts**.

```text
Attempt 1
   │
   ├── Valid → SUCCESS
   │
   └── Invalid
          │
          ▼
      Retry Prompt
          │
          ▼
Attempt 2
   │
   ├── Valid → SUCCESS
   │
   └── Invalid
          │
          ▼
      Retry Prompt
          │
          ▼
Attempt 3
   │
   ├── Valid → SUCCESS
   │
   └── Invalid → FAILURE
```

---

# ⚙️ How It Works

The complete application flow is:

```text
User enters support ticket
          │
          ▼
   ExtractionService
   builds LLM prompt
          │
          ▼
      ILlmClient
          │
          ▼
     OllamaClient
          │
          ▼
   Ollama Local LLM
          │
          ▼
      Raw Response
          │
          ▼
       JsonParser
          │
          ├── Invalid JSON
          │       │
          │       ▼
          │   Retry Prompt
          │
          ▼
   SupportTicket Object
          │
          ▼
    TicketValidator
          │
          ├── Invalid Data
          │       │
          │       ▼
          │   Retry Prompt
          │
          ▼
   ExtractionResult
          │
          ▼
   ConsoleDisplay
```

---

# 🏗️ Architecture

The application follows a simple layered architecture.

```text
┌─────────────────────────┐
│    ConsoleDisplay       │
│        UI Layer         │
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│   ExtractionService     │
│   Orchestration Layer   │
└─────┬─────────┬─────────┘
      │         │
      ▼         ▼
┌──────────┐ ┌─────────────────┐
│JsonParser│ │TicketValidator  │
│ Parsing  │ │  Validation     │
└──────────┘ └─────────────────┘
      │
      ▼
┌─────────────────────────┐
│      SupportTicket      │
│       Domain Model      │
└─────────────────────────┘

ILlmClient
    │
    ├── OllamaClient
    │      Production
    │
    └── FakeLlmClient
           Unit Tests
```

The `ILlmClient` abstraction allows the application to replace the real Ollama client with a deterministic fake during unit testing.

---

# 📁 Project Structure

```text
structured-output-llm-console/
│
├── StructuredOutputDemo/
│   │
│   ├── Program.cs
│   │
│   ├── Models/
│   │   ├── SupportTicket.cs
│   │   ├── ExtractionResult.cs
│   │   └── ValidationResult.cs
│   │
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   └── ILlmClient.cs
│   │   │
│   │   ├── OllamaClient.cs
│   │   ├── JsonParser.cs
│   │   ├── ParseResult.cs
│   │   ├── TicketValidator.cs
│   │   └── ExtractionService.cs
│   │
│   └── UI/
│       └── ConsoleDisplay.cs
│
├── StructuredOutputDemo.Tests/
│   │
│   ├── Fakes/
│   │   └── FakeLlmClient.cs
│   │
│   └── Tests/
│       ├── JsonParserTests.cs
│       ├── TicketValidatorTests.cs
│       └── ExtractionServiceTests.cs
│
├── StructuredOutputLLM.slnx
│
└── README.md
```

---

# 🔧 Prerequisites

Before running the project, install:

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Ollama](https://ollama.com/download)

Verify .NET:

```bash
dotnet --version
```

Verify Ollama:

```bash
ollama --version
```

---

# 🦙 Ollama Setup

Pull a supported model:

```bash
ollama pull llama3.2:latest
```

Start the Ollama server:

```bash
ollama serve
```

Verify installed models:

```bash
ollama list
```

The application communicates with Ollama through:

```text
http://localhost:11434
```

The default model is:

```text
llama3.2:latest
```

The model can be changed in:

```text
StructuredOutputDemo/Services/OllamaClient.cs
```

For example:

```csharp
private const string Model = "llama3.2:latest";
```

Other locally available models can be used depending on your hardware.

---

# 🚀 Running the Application

Make sure Ollama is running:

```bash
ollama serve
```

Then run:

```bash
dotnet run --project StructuredOutputDemo
```

Enter a support ticket such as:

```text
My credit card payment failed three times today. I need to complete my order urgently.
```

The application sends the request to the LLM and attempts to produce:

```json
{
  "category": "Payment",
  "priority": "High",
  "summary": "Customer's credit card payment failed three times and requires urgent assistance."
}
```

The result is then parsed and validated.

Example console output:

```text
============================================
   SUPPORT TICKET STRUCTURED EXTRACTION
============================================

Enter support ticket:
My credit card payment failed three times today.

Structured Result:

Category : Payment
Priority : High
Summary  : Customer payment failed multiple times.

Validation: SUCCESS
Attempts  : 1
```

The application can continue processing additional tickets until the user exits.

---

# 🧪 Running the Tests

The unit tests do **not require Ollama to be running**.

The tests use `FakeLlmClient`, which provides deterministic LLM responses.

Run all tests:

```bash
dotnet test
```

For detailed output:

```bash
dotnet test --verbosity normal
```

Expected result:

```text
Total tests: 12
Passed: 12
Failed: 0
Skipped: 0
```

The exact execution time may vary depending on the machine and test environment.

---

# 🔬 Test Suite

The test suite is divided into three layers.

## Parsing Layer

### `JsonParserTests`

Tests the conversion of raw LLM output into structured C# objects.

| Test            | Scenario                    | Expected Result                                  |
| --------------- | --------------------------- | ------------------------------------------------ |
| Valid JSON      | All required fields present | Parse succeeds                                   |
| Malformed JSON  | Invalid JSON syntax         | Parse fails safely                               |
| Missing field   | `summary` omitted           | Parse succeeds, validation catches missing value |
| Wrong data type | `priority` is a number      | Parse fails safely                               |

---

## Validation Layer

### `TicketValidatorTests`

Tests application-level data validation.

Examples:

```text
Empty Category
Empty Priority
Empty Summary
All fields populated
```

The validator ensures that successfully parsed JSON also contains the required application data.

---

## Orchestration Layer

### `ExtractionServiceTests`

Tests the complete extraction and retry workflow.

| Scenario                            | Expected LLM Calls |
| ----------------------------------- | -----------------: |
| Empty user input                    |                  0 |
| Valid first response                |                  1 |
| Invalid response → valid retry      |                  2 |
| Three invalid responses             |                  3 |
| Malformed response → valid retry    |                  2 |
| Full successful extraction pipeline |                  1 |

The retry limit is enforced at:

```text
Maximum Attempts = 3
```

The `FakeLlmClient` also throws an exception if the application attempts to call it more times than the test expects. This helps verify that the retry mechanism does not accidentally exceed its limit.

---

# 🔁 Retry Mechanism

A simplified example:

### First LLM response

```json
{
  "category": "Payment"
}
```

The parser succeeds because the JSON is syntactically valid.

The validator then detects:

```text
Missing required field: priority
```

The application generates a retry prompt explaining the problem.

The model is called again.

### Second LLM response

```json
{
  "category": "Payment",
  "priority": "High",
  "summary": "Customer payment failed."
}
```

The response is parsed and validated successfully.

Result:

```text
SUCCESS
Attempts: 2
```

If all three attempts fail:

```text
Extraction FAILED
Reason: Maximum retry attempts exceeded
```

---

# 🎯 Key Design Decisions

| Design Decision          | Reason                                                   |
| ------------------------ | -------------------------------------------------------- |
| `ILlmClient` interface   | Separates LLM infrastructure from business logic         |
| `OllamaClient`           | Provides the production implementation using Ollama      |
| `FakeLlmClient`          | Enables deterministic unit testing without Ollama        |
| `JsonParser`             | Separates JSON syntax handling from validation           |
| `TicketValidator`        | Ensures parsed data meets application requirements       |
| `ParseResult`            | Represents parsing success and failure states explicitly |
| `ExtractionResult`       | Represents the final extraction pipeline result          |
| `AttemptCount`           | Allows retry behavior to be verified                     |
| `FailureReason`          | Provides useful information for retry prompts            |
| Manual dependency wiring | Keeps the project simple and focused on LLM concepts     |
| Maximum 3 attempts       | Prevents uncontrolled retry loops                        |

---

# 📚 Concepts Covered

This project demonstrates the following LLM engineering concepts:

### Structured Output

Instructing an LLM to return machine-readable JSON.

### Reliable JSON Extraction

Handling malformed and unexpected LLM responses safely.

### JSON Parsing

Using `System.Text.Json` to deserialize model output.

### Schema / Data Validation

Ensuring the parsed object contains the required application fields.

### Retry Mechanisms

Giving the model another opportunity to correct invalid output.

### Error Feedback

Providing the detected failure reason in the retry prompt.

### LLM Abstraction

Using an interface to separate application logic from the LLM provider.

### Deterministic Testing

Testing LLM-dependent workflows without making real LLM calls.

### Retry Limit Testing

Verifying that the application does not exceed the configured maximum attempts.

---

# 🚫 Current Scope

This project intentionally focuses only on structured output and reliable JSON extraction.

The following topics are **outside the current scope**:

* Function Calling
* Tool Calling
* MCP (Model Context Protocol)
* AI Agents
* Agent orchestration
* Streaming responses
* Conversation memory
* Multi-agent workflows
* RAG

These concepts can be explored in separate projects or future iterations.

---

# 🔮 Future Extensions

Possible future improvements include:

* Native structured-output support from models that provide JSON schema enforcement
* External JSON Schema validation
* Support for multiple LLM providers
* Configurable model selection
* Configurable retry limits
* Exponential backoff for retries
* Retry telemetry and metrics
* Structured logging
* Response latency tracking
* Token usage tracking
* Integration tests against a real Ollama instance
* Function calling and tool calling
* MCP integration
* Agent-based workflows

---

## 📌 Learning Outcome

The primary lesson of this project is:

> **Never assume that an LLM response is valid just because the model was instructed to return JSON.**

A reliable LLM application should treat the model's output as untrusted input:

```text
User Input
    ↓
LLM
    ↓
Parse
    ↓
Validate
    ↓
Retry if Necessary
    ↓
Return Trusted Structured Data
```

This pattern provides a foundation for building more reliable LLM-powered applications and is an important building block for future work involving **function calling, tool calling, RAG, and AI agents**.
