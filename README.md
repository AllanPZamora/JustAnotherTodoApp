# JustAnotherTodoApp

A personal task and calendar manager for Windows, built with WPF and .NET 10. It supports multiple user profiles, recurring tasks, and multiple color themes — all stored locally as JSON files with no database required.

---

## Features

### User Profiles
- Create multiple profiles, each with a name and a randomly assigned color avatar
- Each profile has its own independent task list and theme setting
- Switch between profiles from the profile selection screen on startup

### Calendar View
- View your tasks on a **monthly** or **weekly** calendar
- Dates that have tasks are marked with a small dot indicator
- Click any date to open a side panel showing that day's tasks

### Task Management
- Add tasks with a **title** and optional **notes**
- Mark tasks as complete using the checkbox on each card
- Delete tasks with the ✕ button

### Recurring Tasks
- Set a task to repeat **daily**, **weekly**, or **monthly**
- Optionally set an **end date** for the recurrence
- When deleting a recurring task, choose to remove only that occurrence or all future ones

### Tasks Dashboard
Organize your work in a dedicated dashboard view split into three sections:
- **Today** — tasks due today
- **Upcoming** — tasks scheduled in the next 7 days
- **Backlog** — old uncompleted tasks (older than 7 days)

Click any task card to see its full details on the right panel.

### Themes
- Multiple color themes available (default: Dark)
- Theme preference is saved per profile

## Data Storage

The app saves all data locally as JSON files — no database or internet connection needed.

**Storage location:**
```
C:\Users\<YourUsername>\AppData\Roaming\TodoCalendarApp\
```

**Files:**

| File | Contents |
|---|---|
| `profiles.json` | All user profiles (name, color, theme, ID) |
| `tasks.json` | All tasks across all profiles (title, notes, date, recurrence, completion state) |

The folder and files are created automatically the first time you save data. You can back up your data by copying this folder.

### Example `tasks.json` structure
```json
[
  {
    "Id": "abc-123",
    "ProfileId": "profile-456",
    "Title": "Buy groceries",
    "Notes": "Milk, eggs, bread",
    "IsCompleted": false,
    "Date": "2026-04-19T00:00:00",
    "Recurrence": "None",
    "RecurrenceEndDate": null,
    "RecurrenceOverrides": {}
  }
]
```

## Project Structure

```
todoApp/
├── MainWindow.xaml / .cs       # Main calendar and task UI
├── ProfileWindow.xaml / .cs    # Profile selection screen
├── SettingsPopup.xaml / .cs    # Per-profile settings (theme, delete)
├── AddProfileDialog.xaml / .cs # Dialog for creating a new profile
├── SplashWindow.xaml / .cs     # Splash/loading screen
├── TaskItem.cs                 # Task data model
├── TaskService.cs              # Task CRUD and recurrence logic
├── Profile.cs                  # Profile data model
├── ProfileService.cs           # Profile save/load logic
├── ThemeService.cs             # Theme definitions and helpers
└── GridLengthAnimation.cs      # Animation helper for panel sliding
```
