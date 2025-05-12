# Advanced Educational Material Sorter (AEMS) - MVP

## Overview
A Windows desktop application to help educators organize digital learning materials by AI-driven analysis and linking to a curriculum structure extracted from user-provided PDFs.

## Technology Stack
- Language & Framework: C# with .NET 7+ using WPF or WinUI 3
- File Parsing: PdfSharp/iTextSharp for PDFs, Open XML SDK for Office files
- HTTP Communication: HttpClient for local AI API
- Data Storage: SQLite
- Logging: .NET built-in logging framework

## Core Modules
1. Curriculum Processing Module
2. Material Ingestion & Analysis Module
   - Extended to support image and video files.
   - Initial categorization based on filenames and folder context.
   - Content analysis fallback if contextual categorization fails.
3. Categorization & Linking Engine
4. User Interface
5. Configuration & Error Handling

## Project Structure
- UI Layer (MVVM pattern)
- Business Logic Layer
- Data Access Layer
- AI Communication Layer

## Future Enhancements
- Manual curriculum editing
- OpenAI fallback integration
- Support for more file types
- Advanced search and performance optimizations

## Development Notes
- Robust error handling and logging
- Background processing with progress reporting
- Persistent user settings
