import { Navigate, NavLink, Route, Routes, useNavigate } from "react-router-dom";
import { DashboardPage } from "./pages/DashboardPage";
import { DriverDetailPage } from "./pages/DriverDetailPage";
import { DriversPage } from "./pages/DriversPage";
import { LoginPage } from "./pages/LoginPage";
import { PaymentsPage } from "./pages/PaymentsPage";
import { RoutesPage } from "./pages/RoutesPage";
import { StudentsPage } from "./pages/StudentsPage";
import { SubscriptionsPage } from "./pages/SubscriptionsPage";
import { UsersPage } from "./pages/UsersPage";
import { VehiclesPage } from "./pages/VehiclesPage";

function Layout() {
  const navigate = useNavigate();
  if (!localStorage.getItem("token")) return <Navigate to="/login" replace />;

  function logout() {
    localStorage.removeItem("token");
    navigate("/login");
  }

  return (
    <>
      <nav>
        <NavLink to="/">Dashboard</NavLink>
        <NavLink to="/drivers">Drivers</NavLink>
        <NavLink to="/subscriptions">Subscriptions</NavLink>
        <NavLink to="/vehicles">Vehicles</NavLink>
        <NavLink to="/students">Students</NavLink>
        <NavLink to="/routes">Routes</NavLink>
        <NavLink to="/payments">Payments</NavLink>
        <NavLink to="/users">Access</NavLink>
        <button onClick={logout}>Log out</button>
      </nav>
      <main>
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/drivers" element={<DriversPage />} />
          <Route path="/drivers/:id" element={<DriverDetailPage />} />
          <Route path="/subscriptions" element={<SubscriptionsPage />} />
          <Route path="/vehicles" element={<VehiclesPage />} />
          <Route path="/students" element={<StudentsPage />} />
          <Route path="/routes" element={<RoutesPage />} />
          <Route path="/payments" element={<PaymentsPage />} />
          <Route path="/users" element={<UsersPage />} />
        </Routes>
      </main>
    </>
  );
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/*" element={<Layout />} />
    </Routes>
  );
}

export default App;
