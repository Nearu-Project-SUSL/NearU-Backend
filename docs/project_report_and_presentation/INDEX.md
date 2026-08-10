# NearU Project Documentation & Report Master Suite
## Table of Contents & Navigation Index

Welcome to the comprehensive documentation suite for the **NearU** hyper-local campus service, ride-sharing, accommodation, and gig marketplace ecosystem. This suite has been generated directly from in-depth codebase analysis across all three project repositories:
1. **Backend Service (`NearU-Backend`)**: .NET 10 Web API, PostgreSQL + PostGIS, SignalR WebSockets, Redis, Docker, GitHub Actions CI/CD, AWS Lightsail
2. **Web Application (`NearU-Frontend`)**: React 18, Vite, TypeScript, Tailwind CSS, Material UI, Leaflet Maps, Vercel CDN
3. **Mobile Application (`NearU-Mobile-App`)**: React Native, Expo SDK 54, Expo Router, Native Maps, SecureStore, Expo EAS

---

## Document Directory

| File Name | Document Title | Primary Target Usage |
| :--- | :--- | :--- |
| 📄 [01_Executive_Summary_and_Overview.md](file:///c:/Users/THIMIRA/NearU-Backend/docs/project_report_and_presentation/01_Executive_Summary_and_Overview.md) | **Executive Summary & System Overview** | Project Intro, Problem Statement, Stakeholder Personas |
| 📄 [02_System_Architecture_and_Tech_Stack.md](file:///c:/Users/THIMIRA/NearU-Backend/docs/project_report_and_presentation/02_System_Architecture_and_Tech_Stack.md) | **System Architecture & Tech Stack** | Architecture Diagrams, Layered Patterns, Full Tech Matrix |
| 📄 [03_Database_Schema_and_Data_Model.md](file:///c:/Users/THIMIRA/NearU-Backend/docs/project_report_and_presentation/03_Database_Schema_and_Data_Model.md) | **Database Schema & Data Model** | Mermaid ERD, Entity Tables, PostGIS Spatial Logic |
| 📄 [04_Backend_API_and_Services_Specification.md](file:///c:/Users/THIMIRA/NearU-Backend/docs/project_report_and_presentation/04_Backend_API_and_Services_Specification.md) | **Backend API & Services Specification** | RESTful Endpoints Catalogue, SignalR Hubs, Worker Jobs |
| 📄 [05_Web_Frontend_Application_Details.md](file:///c:/Users/THIMIRA/NearU-Backend/docs/project_report_and_presentation/05_Web_Frontend_Application_Details.md) | **Web Frontend Application Details** | React 18 Routes, Views, Component Trees, Axios Setup |
| 📄 [06_Mobile_App_Details.md](file:///c:/Users/THIMIRA/NearU-Backend/docs/project_report_and_presentation/06_Mobile_App_Details.md) | **Mobile Application Details** | Expo Router Screen Map, Native GPS, SecureStore |
| 📄 [07_Security_Performance_and_Deployment.md](file:///c:/Users/THIMIRA/NearU-Backend/docs/project_report_and_presentation/07_Security_Performance_and_Deployment.md) | **Security, Performance & Deployment** | JWT Refresh Rotation, PostGIS Tuning, Docker & Nginx |
| 📄 [08_Final_Project_Report_Template.md](file:///c:/Users/THIMIRA/NearU-Backend/docs/project_report_and_presentation/08_Final_Project_Report_Template.md) | **Final Academic Project Report Template**| Chapters 1–7 Thesis / Final Degree Project Submission Text |
| 📄 [09_Presentation_Deck_and_Speaker_Notes.md](file:///c:/Users/THIMIRA/NearU-Backend/docs/project_report_and_presentation/09_Presentation_Deck_and_Speaker_Notes.md) | **Presentation Deck & Speaker Notes** | 18-Slide Defense Deck with Visual Prompts & Notes |
| 📄 [10_Hosting_Infrastructure_and_CICD_Pipeline.md](file:///c:/Users/THIMIRA/NearU-Backend/docs/project_report_and_presentation/10_Hosting_Infrastructure_and_CICD_Pipeline.md) | **Hosting Infrastructure & CI/CD Pipeline** | AWS Lightsail, Vercel, Expo EAS, GitHub Actions CI/CD |

---

## System Quick Reference

- **Backend Base URL (Dev)**: `http://localhost:5059/api`
- **Backend Base URL (Prod)**: `https://api.nearusab.me/api`
- **SignalR WebSocket Hub**: `wss://api.nearusab.me/hubs/rides`
- **Scalar Interactive API Docs**: `https://api.nearusab.me/scalar/v1`
- **Web Frontend**: `C:\Users\THIMIRA\NearU-Frontend` (`https://near-u-frontend-pi.vercel.app`)
- **Mobile Application**: `C:\Users\THIMIRA\NearU-Mobile-App` (Expo EAS Project ID `0c9a3978-9c60-4f89-9211-9012d5820aea`)
