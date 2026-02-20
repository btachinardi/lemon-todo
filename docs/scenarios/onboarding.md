# Onboarding Scenarios

> **Source**: Extracted from docs/SCENARIOS.md §5
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Scenario S01: First-Time Registration (Sarah)

**Context**: Sarah heard about LemonDo from a friend. She opens the app on her iPhone.

```
Step 1: Sarah visits lemondo.app on her phone
  -> She sees a clean landing page with "Get started free" button
  -> She notices the app looks modern and not cluttered
  [analytics: landing_page_viewed, device: mobile]

Step 2: Sarah taps "Get started free"
  -> She sees registration options: Email, Google, GitHub
  -> She chooses Google (fastest for her)
  [analytics: signup_cta_clicked, method: google]

Step 3: Google OAuth flow completes
  -> She's redirected back to LemonDo
  -> Her name and email are pre-filled from Google
  -> She sees a "Welcome, Sarah!" screen
  [analytics: registration_completed, method: google]

Step 4: Onboarding begins
  -> A friendly, minimal overlay says "Let's get you started. Create your first task!"
  -> There's a large, inviting input field with placeholder "What do you need to do?"
  -> There's a subtle "Skip tour" link at the bottom
  [analytics: onboarding_started]

Step 5: Sarah types "Design logo for Acme Corp" and hits Enter
  -> The task appears with a satisfying animation
  -> Confetti or subtle celebration effect
  -> Message: "Your first task! Now let's complete it."
  [analytics: onboarding_first_task_created]

Step 6: Sarah taps the checkmark to complete the task
  -> Strikethrough animation, task moves to "Done"
  -> Celebration: "You did it! You're all set."
  -> CTA: "Explore your board" or "Add more tasks"
  [analytics: onboarding_first_task_completed, onboarding_completed]
```

**Expected Emotion**: Delighted, not overwhelmed. "That was easy."

---

## Scenario S03: Email Registration & Password Flow (New User)

**Context**: A user without Google/GitHub accounts registers with email.

```
Step 1: User clicks "Sign up with email"
  -> Form: Name, Email, Password (with strength indicator)
  -> Password requirements shown inline (not as error)
  [analytics: registration_form_started, method: email]

Step 2: User submits the form
  -> Loading spinner on button (no page reload)
  -> Success: "Check your email for a verification link"
  -> Email sent within 5 seconds
  [analytics: registration_submitted, method: email]

Step 3: User clicks verification link in email
  -> Opens LemonDo in browser
  -> "Email verified! Let's get started."
  -> Redirected to onboarding
  [analytics: email_verified]

Step 4: User forgets password later
  -> Clicks "Forgot password?" on login
  -> Enters email -> receives reset link
  -> Sets new password -> logged in automatically
  [analytics: password_reset_requested, password_reset_completed]
```

---

## Scenario S10: First-Time Setup to Productive User (Full Journey)

**Context**: Complete journey from discovery to being a weekly active user.

```
Week 0 - Day 1: Discovery & Signup
  -> Finds LemonDo via recommendation
  -> Signs up with Google OAuth (30 seconds)
  -> Completes onboarding (2 minutes)
  -> Creates 5 tasks
  -> Completes 2 tasks
  -> Feels satisfied: "This is what I needed"
  [north_star: first task completed]

Week 0 - Day 2-3: Habit Formation
  -> Opens app in morning (via PWA on phone)
  -> Adds daily tasks
  -> Discovers priority system
  -> Starts using High/Medium/Low consistently
  [analytics: feature_discovered: priorities]

Week 1: Feature Exploration
  -> Tries Kanban view, finds it useful for project work
  -> Uses list view for quick daily planning
  -> Installs PWA on phone
  -> Switches to dark mode
  [analytics: pwa_installed, theme_toggled, view_switched]

Week 2: Sustained Usage
  -> Opens LemonDo daily
  -> Completing 3-5 tasks per day
  -> Creates tasks from email reminders
  -> Tells a friend about LemonDo
  [north_star: weekly_active_task_completer = true]

Month 2: Power User
  -> Has 50+ completed tasks
  -> Uses tags for different projects
  -> Kanban board is primary view
  -> Recommends to 2 colleagues
  [analytics: referral_sent x2]
```
