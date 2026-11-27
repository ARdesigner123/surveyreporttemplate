MQUEST SERVICE REPORT & ACCEPTANCE SHEET - WEBSITE
==================================================

WEBSITE FILES (these are what go on Vercel)
  index.html     the report page
  customers.js   customer list (name + address for the dropdown)
  logo.jpeg      MQuest logo
  vercel.json    Vercel settings
  (.vercelignore keeps the old desktop files - app.exe, .bat, server.ps1, launcher/ -
   off the website.)

DEPLOY TO VERCEL - option A (website, no commands)
  1. Put this folder in a GitHub repository.
  2. Go to vercel.com -> Add New -> Project -> Import the repository.
  3. Framework Preset: "Other". Leave Build Command and Output Directory empty.
  4. Click Deploy. Vercel gives a link like https://your-project.vercel.app
  Every later push to GitHub updates the website automatically.

DEPLOY TO VERCEL - option B (command line)
  In this folder run:   npx vercel        (log in, accept the defaults)
  Then for the live site:  npx vercel --prod

USING THE WEBSITE
  - Customer: pick from the dropdown -> address fills in automatically.
    "Other (type manually)" for a customer not in the list.
  - Dates: tap to open the calendar (shown as DD/MM/YYYY).
  - Tick one: Covered by Warranty / Chargeable / Testing.
  - Signatures: tap the signature box, sign with finger / stylus / mouse, press Done.
  - Download PDF: saves the filled report as a PDF file
    (named e.g. "Service Report - Haig Court - 25-11-2026.pdf").
    If something is not filled in, it lists the missing fields (in red) first.
  - Print: opens the normal print window (can also "Save as PDF" there).
  - The form is saved automatically in that browser until you press "New Report".
    Nothing is sent to any server - each report stays on the device it was filled on.

ADD OR EDIT CUSTOMERS
  Edit customers.js, then redeploy (or push to GitHub).
