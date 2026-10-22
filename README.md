# Staybnb – Property Booking Management System

Staybnb is a web-based accommodation booking platform developed using **ASP.NET Core MVC and C#**. The project was inspired by Airbnb and aims to provide a simple and convenient way for guests to discover accommodation, make bookings, and communicate with property hosts.

The system supports different user roles, including Guests, Hosts, Admins, and SuperAdmins, with each role having access to specific features.

## Project Overview

The main objective of Staybnb is to create a functional property booking system that allows users to manage accommodation bookings through one platform.

Guests can browse available properties, request bookings, and complete check-in requirements. Hosts can manage their properties, respond to booking requests, and verify guest documentation. Administrators oversee host applications and general system activities.

## Technologies Used

- **C#** – Backend development
- **ASP.NET Core MVC** – Web application framework
- **Entity Framework Core** – Database access and management
- **ASP.NET Core Identity** – User authentication and role management
- **Razor Views** – Frontend page rendering
- **HTML, CSS and JavaScript** – User interface development
- **SQL Database** – Application data storage
- **Visual Studio** – Development environment
- **Git and GitHub** – Version control

## Main Features

### 1. User Authentication and Role Management
- User registration and login
- Secure authentication using ASP.NET Core Identity
- Role-based access control
- Guest-to-Host application process
- Automatic role updates after administrator approval

### 2. Guest Features
- Browse accommodation listings
- View property descriptions and images
- Submit booking requests
- Track booking statuses
- Upload required check-in documents
- Replace rejected documents and resubmit them
- Communicate with hosts through messaging
- View notifications

### 3. Host Features
- Access a dedicated Host Dashboard
- Manage property listings and images
- View incoming booking requests
- Approve or reject bookings
- Review guest check-in documents
- Approve or reject submitted documentation
- Receive booking-related notifications

### 4. Admin Features
- Review Host applications
- Approve or reject applications
- Monitor system activity logs
- Manage administrative processes
- SuperAdmin functionality for assigning Admin roles

### 5. Messaging and Notifications
- Inbox functionality for communication between users
- Notifications for important system activities
- Read and unread message tracking
- Notification statuses stored in the database

## Booking and Check-In Process

The booking process follows a structured workflow:

1. A Guest selects a property and submits a booking request.
2. The booking is initially marked as **Pending**.
3. The Host reviews and approves the booking.
4. The Guest uploads the required check-in documentation.
5. The Host reviews the submitted documents.
6. If documents are rejected, the Guest can replace them and resubmit.
7. Once verification is successful, the booking progresses to **CheckedIn**.

This workflow helps hosts manage reservations and verify guests before check-in.

## Database and Application Structure

Staybnb uses Entity Framework Core for database operations and migrations.

The application follows the Model-View-Controller (MVC) architecture:

- **Models:** Represent application data and business entities.
- **Views:** Display information and provide user interfaces.
- **Controllers:** Handle requests, application logic, and user interactions.
- **Data:** Contains database configuration, migrations, and supporting data operations.
- **Services:** Support reusable application functionality.

Activity logging is included in important system operations to help administrators monitor actions.

## Running the Project

To run Staybnb locally:

1. Clone the repository:
   ```bash
   git clone https://github.com/sadinkie/Staybnb.git
   ```
2. Open the project solution in Visual Studio.
3. Restore the required NuGet packages.
4. Configure the database connection string in the appropriate local configuration.
5. Apply any required Entity Framework Core migrations.
6. Run the project using Visual Studio.

**Note:** Local database settings, development credentials, and other environment-specific configuration may need to be adjusted before running the application.

## Project Purpose

Staybnb was developed as an academic software development project to demonstrate practical skills in:

- ASP.NET Core MVC application development
- Database design and management
- User authentication and authorization
- Role-based access control
- File uploads and document verification
- Booking workflow management
- Messaging and notifications
- Git and GitHub version control

The project also provided an opportunity to apply software development concepts to a realistic accommodation management scenario.

## Future Improvements

Possible future enhancements include:

- Integration with an online payment gateway
- Interactive property location maps
- Guest reviews and property ratings
- Advanced property search and filtering
- Email and SMS notifications
- Improved mobile responsiveness

## Project Status

**Status:** Functional academic project

The main booking, host management, administration, messaging, and check-in workflows have been implemented and tested during development.

## Developer

Developed as part of an academic project using C#, ASP.NET Core MVC, and Entity Framework Core.
