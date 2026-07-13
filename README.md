# IT-Service-Portal

A XAML-based desktop tool designed to manage and track user support requests in a simulated school environment. Built using C# and SQL, this application was developed for the NCEA Level 3 Digital Technologies Internal Assessment.
* [Standard 91902: Use complex techniques to develop a database](https://www.nzqa.govt.nz/ncea/assessment/view-detailed.do?standardNumber=91902)
* [Standard 91907: Use complex processes to develop a digital technologies outcome](https://www.nzqa.govt.nz/ncea/assessment/view-detailed.do?standardNumber=91907)

<img width="1102" height="636" alt="image" src="https://github.com/user-attachments/assets/dd486d7c-b810-421e-8233-98509e35f1dc" />

## Prerequisites

To run this application locally, you will need:
* **.NET Runtime / Visual Studio** (to build and run the C# XAML application)
* **MySQL Server** 
* **MySQL Workbench** (or any preferred database management tool)

## Database Setup

Because the original database was hosted externally, you will need to host a local instance of the database using the provided schema:

1. Open **MySQL Workbench** and connect to your local MySQL instance.
2. Import and run the database schema script found inside the `3.3 3.8 Internal Assessment` folder to generate the required tables.
3. Open the project source code and update the `connectionString` with your local database credentials (host, username, and password) for the program to connect successfully.

## How to Run
1. Clone the repository to your local machine.
2. Open the solution file in Visual Studio.
3. Ensure the database connection string is configured correctly.
4. Build and run the project.
