# HabitKit Clone

A complete clone of the HabitKit app built with ASP.NET Core Blazor Server, featuring GitHub-style contribution graphs and full settings management.

## Features

### ✅ **Completed Features:**
- **GitHub-Style Contribution Calendar**: Each habit displays a year-long contribution graph exactly like GitHub's
- **Interactive Habit Tracking**: Click any day to toggle habit completion
- **Real-time Updates**: Changes reflect immediately in the UI
- **User Authentication**: Email/password and Google OAuth support
- **Settings Management**: Complete user settings with database persistence
- **Theme Switching**: Light and dark themes with persistent storage
- **Language Support**: English and Vietnamese with seamless switching
- **Responsive Design**: Works perfectly on all screen sizes
- **Exact UI Replication**: Matches HabitKit's design precisely

### 🎯 **Settings Functionality:**
- **Language Switching**: Change between English and Vietnamese
- **Theme Toggle**: Switch between light and dark themes
- **Notification Preferences**: Control email and in-app notifications
- **Daily Reminder Time**: Set custom reminder times
- **Real-time Updates**: Settings save automatically as you change them
- **Persistent Storage**: All settings are saved to the database
- **Visual Feedback**: Toast notifications confirm successful changes

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Visual Studio Code or Visual Studio 2022

### Running the Application

1. **Clone and navigate to the project:**
   ```bash
   cd /Users/datmai/Code/datnet-habitkit/HabitKitClone
   ```

2. **Restore packages:**
   ```bash
   dotnet restore
   ```

3. **Run the application:**
   ```bash
   dotnet run
   ```

4. **Open your browser:**
   Navigate to `https://localhost:5126` or `http://localhost:5126`

### Testing Settings

1. **Register a new account** or use the demo account:
   - Email: `demo@habitkit.com`
   - Password: `Demo123!`

2. **Navigate to Settings:**
   - Click on "Settings" in the navigation menu
   - You'll see the complete settings page with all options

3. **Test Settings Functionality:**
   - **Language**: Change between English and Vietnamese (page reloads automatically)
   - **Theme**: Switch between light and dark themes (applies immediately)
   - **Notifications**: Toggle email and in-app notifications (saves automatically)
   - **Reminder Time**: Change the daily reminder time (saves automatically)
   - **Save All**: Use the "Save" button to save all settings at once

4. **Verify Persistence:**
   - Refresh the page or log out and back in
   - Your settings should be preserved

### Demo Data

The application comes pre-loaded with:
- Demo user account
- Sample habits with completion data
- User settings with default values

## Technical Details

### Architecture
- **Framework**: ASP.NET Core 9.0 with Blazor Server
- **Database**: SQLite with Entity Framework Core
- **Authentication**: ASP.NET Core Identity with Google OAuth
- **UI**: Custom CSS with Bootstrap 5
- **State Management**: Blazor Server with dependency injection

### Key Services
- `IHabitService`: Manages habit CRUD operations and completions
- `IUserSettingsService`: Handles user settings persistence
- `UserContextService`: Provides current user context
- `DataSeeder`: Seeds the database with demo data

### Database Models
- `Habit`: Represents user habits with metadata
- `HabitCompletion`: Tracks daily habit completions
- `UserSettings`: Stores user preferences and settings
- `ApplicationUser`: ASP.NET Identity user model

## Settings Implementation

The settings functionality includes:

1. **Database Persistence**: All settings are stored in the `UserSettings` table
2. **Real-time Updates**: Individual settings save immediately when changed
3. **Cookie Management**: Language and theme preferences are stored in cookies
4. **JavaScript Integration**: Theme switching uses JavaScript for immediate visual feedback
5. **Error Handling**: Comprehensive error handling with user-friendly messages
6. **Loading States**: Visual feedback during save operations

## Troubleshooting

### Common Issues

1. **Port Already in Use**:
   ```bash
   lsof -ti:5126 | xargs kill -9
   ```

2. **Database Issues**:
   ```bash
   dotnet ef database drop
   dotnet ef database update
   ```

3. **Build Errors**:
   ```bash
   dotnet clean
   dotnet build
   ```

## Contributing

This is a complete HabitKit clone with exact UI replication. All core functionality is implemented and working.

## License

This project is for educational purposes only.