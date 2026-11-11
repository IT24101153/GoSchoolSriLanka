
# Sri Lankan School Van Management System
## Project Requirements Specification

### 1. System Overview
The School Van Management System is a comprehensive platform designed to streamline student transportation in Sri Lanka. It connects parents with reliable school van drivers, digitizes the seat booking process, and simplifies monthly fee tracking. The system features a mobile application with distinct driver and parent interfaces, managed by a centralized web portal for administrative oversight. Drivers pay GoSchool a monthly platform subscription to remain listed; this is separate from the hire fee parents pay drivers for the van service itself.

### 2. Technology Stack
*   **Mobile Frontend (Driver & Parent):** Flutter (Cross-platform iOS and Android)
*   **Backend API:** ASP.NET Core Web API 
*   **Database:** PostgreSQL (Relational data management for transactions and complex queries)
*   **Admin Web Portal:** React / Next.js (For responsive and fast web accessibility)

### 3. User Roles and Modules

#### 3.1 Driver Module (Mobile App)
*   **Registration & Verification:** Drivers sign up and input personal details. They must upload photos of the van/bus, vehicle registration number, and specify the maximum passenger capacity. Account stays in "Pending" status until an admin approves it.
*   **Platform Subscription:** Once approved, drivers must keep a monthly subscription payment to GoSchool current to stay listed and operational. Admin tracks each driver's subscription status (Active/Overdue) separately from the hire fees parents pay the driver.
*   **Route & Pricing Configuration:** Drivers define one or more routes. Each route is an ordered list of stops; every stop specifies a school, a pickup location, and a scheduled arrival time. Drivers set the fee structure per route (e.g., LKR per day, or a flat monthly rate per student).
*   **Application Management:** When a parent applies for a seat, the driver receives a push notification. The driver reviews the student's pickup location and school, and chooses to **Accept** or **Decline** the request based on current bus capacity.
*   **Payment & Bank Details:** Drivers input their local bank account details (Bank Name, Branch, Account Number). The app tracks which parents have paid for the month and who is pending.
*   **Payment Reminders:** Drivers can send a payment-due notification to any parent who hasn't paid for the current month.
*   **Manual Payment Entry:** If a parent pays via cash in the van, the driver can manually log the payment to keep the system balanced.

#### 3.2 Parent Module (Mobile App)
*   **Parent Registration & Student Profiles:** Parents create an account, providing their own home location (address and coordinates). They can add multiple student profiles; each profile requires the student's name, school (with the school's location), and primary/emergency contact numbers.
*   **Search & Discovery:** Parents browse or search for drivers already registered in the system, matched by proximity between the parent's home location and the driver's route stops, and/or filtered by the student's school. Each result shows the route's stops, schools covered, and pricing.
*   **Seat Application:** Parents send a seat request to a selected driver for a specific student. Once accepted by the driver, the finalized pricing and the student's scheduled pickup arrival time are unlocked and displayed.
*   **Driver & Arrival Visibility:** Once accepted, parents can view the driver's profile and the scheduled arrival time for their student's pickup stop.
*   **Payment Processing:**
    *   **Bank Transfer:** The app displays the driver's bank details. Parents can perform a bank transfer and upload the receipt/slip as proof within the app.
    *   **Cash Payment Logging:** Parents can mark a payment as "Paid via Cash", which sends a confirmation prompt to the driver.

#### 3.3 Admin Portal (Web Portal)
*   **Master Dashboard:** A high-level overview of total active drivers, registered students, total vehicles, and driver subscription stats (active / overdue / monthly revenue).
*   **Driver Approval Workflow:** All newly registered drivers remain in a "Pending" state until an admin reviews their vehicle photos and details. Admins can **Approve** or **Reject** driver accessibility to ensure safety and quality control.
*   **Driver Subscription Management:** Admins view each driver's platform subscription history and status, and can mark a subscription paid or suspend a driver whose subscription is overdue.
*   **System Oversight:** Admins have read-only access to view all driver profiles, vehicle details, student lists assigned to specific vans, and routing/stop information.
*   **Access Management:** Ability to block/suspend drivers or parents who violate terms.

### 4. Key Workflows
1.  **Driver Onboarding:** Driver registers (personal + vehicle details) -> account is "Pending" -> admin approves/rejects -> if approved, driver can configure routes/stops and go live; subscription payment keeps the account active going forward.
2.  **Parent Onboarding:** Parent registers with home location -> adds student profile(s) with each student's school and school location.
3.  **Discovery & Application:** Parent searches/browses drivers near their home location and/or student's school -> applies for a seat for a student -> driver accepts/declines -> on acceptance, pricing and pickup arrival time are unlocked for the parent.
4.  **Monthly Billing Cycle (two independent streams):**
    *   Parent -> Driver: hire fee for the van service (Cash or Bank Transfer, tracked per student/application).
    *   Driver -> Platform: subscription fee to remain listed (tracked by admin).
5.  **Payment Reminders:** Driver triggers a reminder notification to any parent with an unpaid balance for the current month.

### 5. Database Schema Requirements (High-Level)
*   **Users Table:** Role-based access (Admin, Driver, Parent). Parent rows store a home address/coordinates.
*   **Vehicles Table:** Linked to Driver ID, stores capacity, photos, and plate numbers.
*   **Students Table:** Linked to Parent ID, stores school name, school address/coordinates, and emergency contacts.
*   **Routes Table:** A driver's route and its pricing (per day / per month).
*   **RouteStops Table:** Linked to Route ID; ordered stops, each with a school, pickup location, and scheduled arrival time.
*   **Applications Table:** Tracks seat requests per student/route (Status: Pending, Accepted, Declined).
*   **Payments Table:** Tracks monthly parent-to-driver transactions (Method: Cash/Bank, Status: Pending, Completed, Slip Image URL).
*   **DriverSubscriptions Table:** Tracks monthly driver-to-platform transactions (BillingYear, BillingMonth, Amount, Status: Pending/Completed/Overdue, PaidAt).

### 6. Development Environment Recommendations
For a streamlined local development experience, utilizing a `.vscode/tasks.json` configuration is highly recommended. This allows you to simultaneously spin up the ASP.NET Core backend API, the Next.js admin web portal, and the Flutter mobile emulator with a single command, ensuring seamless full-stack debugging and rapid iteration.
