# GDG Community Dashboard 🚀

Welcome to the **Google Developer Groups (GDG) Community Dashboard**. This platform is designed specifically to manage, scale, and track community members, learning roadmaps, and study groups effortlessly.

Built with a robust **ASP.NET Core 10 MVC 3-Tier Architecture**, this system provides a unified interface for Admins to monitor community growth and Instructors to manage deep-dive tech groups and educational roadmaps.

---

## 🏗 System Architecture

The project strictly follows a **3-Tier Architecture** for scalability, testability, and separation of concerns:

1. **`GDG DashBoard` (UI Layer):** 
   - Responsible for routing, controllers, and views. 
   - Implements authentication policies.
   - Dual-styling approach:
     - The public-facing pages (Students/Home) use **Bootstrap 5**.
     - The Admin & Instructor Consoles (`_AdminLayout`) are heavily customized using **Tailwind CSS** to match Google's modern "Bento Grid" aesthetic, alongside Material Symbols.
2. **`GDG DashBoard.BLL` (Business Logic Layer):**
   - Houses the core logic of the application via Services (`GroupService`, `RoadmapService`, `AdminService`, `RoleService`, `AuthService`, `EmailService`).
   - Ensures data integrity (e.g., verifying if a student is already enrolled before syncing progress).
3. **`GDG DashBoard.DAL` (Data Access Layer):** 
   - Built on Entity Framework Core (`AppDbContext`).
   - Uses the Generic Repository and Unit of Work patterns (`IGenericRepositoryAsync`).
   - Contains all Domain Models (`ApplicationUser`, `CommunityGroup`, `Roadmap`, `RoadmapLevel`, `Resource`, `UserNodeProgress`, etc.).

---

## 🌟 Core Features

### 1. 🛡 Role-Based Access Control (RBAC)
The system seeds default Identity Roles on startup:
* **Admin**: Has overarching access, can view the Community Growth dashboard, onboard new members, and manage roles globally.
* **Instructor**: Dedicated access to the **Instructor Console**. Can lead groups, build roadmaps, and monitor student progression.
* **Member**: Standard students who can join groups and track their learning progress.
* **Speaker / Organizer / Mentor**: Specialized roles for GDG events and leadership.

### 2. 👥 Classroom & Group Management
Instructors can create specialized study cohorts.
* **Auto-Join Codes & QR Integration:** Every group generates a unique **6-character `JoinCode`** (e.g., `A3X9B1`).
* Instructors are provided with an "Invite Students" card featuring a **dynamically generated QR Code** (via `api.qrserver.com`) and a 1-click copyable invite link.
* **Seamless Onboarding:** When a student accesses the link, they see a beautiful form to enter the Group. Upon joining, they are instantly added to the active **Roadmap** and their progress tracking initializes from 0%.

### 3. 🗺 Roadmap Builder (Content Management)
A complete curriculum management tool.
* **Roadmaps:** Top-level learning tracks (e.g., "Backend Developer Journey", 20 hours, Beginner).
* **Levels (Modules):** Roadmaps are broken down into logical steps (e.g., "Week 1: OOP", "Chapter 2: APIs").
* **Resources:** Instructors add hyper-specific learning materials into Levels. Resources can be:
  * `VideoCourse` 🎥
  * `Article` 📄
  * `PracticalTask` 💻
  * `Quiz` 🧠
* **Real-time Progress Sync Engine:** In `RoadmapService.cs`, the system acts smartly. If an Instructor adds a *new Level* to an existing Roadmap, the system immediately queries all students enrolled across all groups and provisions `UserNodeProgress` records for them so their curriculums are instantly updated without manual refreshes!

### 4. 📊 The Bento-Grid Dashboard
The UI for management uses high-end, dynamic Tailwind components featuring:
* Progress bars showing precise completion percentages for members.
* Quick metrics: Total Groups, Active Roadmaps, Total Managed Members.
* Badges mapped to enums (Beginner, Intermediate) utilizing specific color palettes (Google Blue, Red, Yellow, Green).

---

## 🛠 Tech Stack

* **Framework:** .NET 10 SDK
* **Web:** ASP.NET Core MVC
* **Database:** SQL Server (via EF Core)
* **Design & CSS:** Tailwind CSS (Admin/Instructor Consoles), Bootstrap 5 (Public pages / Landing)
* **Icons:** Google Material Symbols Outlined
* **Alerts & Modals:** SweetAlert2
* **QR Codes:** External API integration (`api.qrserver.com`)

---

## 🚀 Getting Started

1. **Clone & Configure:**
   Open `e:\GDG-Dashboard\GDG DashBoard\appsettings.json` and ensure your `DefaultConnection` points to your local SQL Server instance.
2. **Database Migrations:**
   Ensure the database is up-to-date with your Models:
   ```bash
   dotnet ef database update --project "GDG DashBoard.DAL" --startup-project "GDG DashBoard"
   ```
3. **Run the Project:**
   ```bash
   cd "e:\GDG-Dashboard\GDG DashBoard"
   dotnet watch run
   ```
4. **Initial Login:**
   The `Program.cs` automatically seeds an Admin user upon startup (check `AdminSettings` in your `appsettings.json` for credentials). Log in with those details to access the Management dashboard!

---
*Built with ❤️ for the GDG Community.*
