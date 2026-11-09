import { NavLink, Route, Routes } from "react-router-dom";
import { DashboardPage } from "./pages/DashboardPage";
import { DriversPage } from "./pages/DriversPage";

function App() {
  return (
    <>
      <nav>
        <NavLink to="/">Dashboard</NavLink>
        <NavLink to="/drivers">Drivers</NavLink>
      </nav>
      <main>
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/drivers" element={<DriversPage />} />
        </Routes>
      </main>
    </>
  );
}

export default App;
