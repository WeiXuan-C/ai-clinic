# AI Clinic - System Architecture & Design Documentation

## Executive Summary

AI Clinic is a healthtech platform that combines AI-powered medical assistance with real doctor consultations. The system enables patients to get instant answers through an AI chatbot trained on official medical documents, while also providing access to real doctors from various organizations for personalized care.

## Core Features

1. **AI Chatbot with Document Grounding**
   - Patients upload medical documents
   - AI analyzes documents using vector embeddings
   - Patients receive accurate, document-backed answers with citations

2. **OTP-Based Authentication with Auto-Registration**
   - Passwordless login via OTP (email)
   - Automatic user registration on first login
   - Role-based access (Patient, Doctor, Admin)

3. **Multi-Organization Doctor Network**
   - Doctors from different organizations can join
   - Real-time availability status
   - Smart routing to available doctors based on specialization and workload

4. **Hybrid Chat System**
   - AI chatbot for instant responses
   - Automatic escalation to human doctors when available
   - Seamless handoff between AI and human support
   - Context preservation across AI-to-human transitions

---

## System Architecture

### Technology Stack

#### Backend
- **Framework**: ASP.NET Core 8.0 (Blazor Server)
- **Language**: C# 12
- **Database**: Supabase (PostgreSQL)
- **Real-time**: Supabase Realtime for live chat updates

#### Frontend
- **Framework**: Blazor Server Components
- **UI Library**: Custom Stitch Design System
- **State Management**: Singleton State Pattern
