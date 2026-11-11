import { useFetch } from "../api/useFetch";
import type { Route, User } from "../types";

export function RoutesPage() {
  const { data, error } = useFetch<{ driver: User; route: Route }[]>("/admin/routes");
  return (
    <section>
      <h1>Routes</h1>
      {error && <p role="alert">{error}</p>}
      {data?.map(({ driver, route }) => (
        <div key={route.id}>
          <h2>
            {route.name} — {driver.fullName} ({driver.status})
          </h2>
          <p>
            LKR {route.pricePerDay}/day · LKR {route.pricePerMonth}/month
          </p>
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>School</th>
                <th>Pickup</th>
                <th>Location</th>
                <th>Arrival</th>
              </tr>
            </thead>
            <tbody>
              {route.stops.map((s) => (
                <tr key={s.id}>
                  <td>{s.sequence}</td>
                  <td>{s.school}</td>
                  <td>{s.pickupAddress}</td>
                  <td>
                    {s.latitude}, {s.longitude}
                  </td>
                  <td>{s.arrivalTime.slice(0, 5)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}
      {data?.length === 0 && <p>No routes yet.</p>}
    </section>
  );
}
