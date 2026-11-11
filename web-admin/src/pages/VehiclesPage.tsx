import { useFetch } from "../api/useFetch";
import type { User, Vehicle } from "../types";

export function VehiclesPage() {
  const { data, error } = useFetch<(Vehicle & { driver: User })[]>("/admin/vehicles");
  return (
    <section>
      <h1>Vehicles</h1>
      {error && <p role="alert">{error}</p>}
      <table>
        <thead>
          <tr>
            <th>Plate</th>
            <th>Capacity</th>
            <th>Driver</th>
            <th>Driver status</th>
            <th>Approval</th>
            <th>Photos</th>
          </tr>
        </thead>
        <tbody>
          {data?.map((v) => (
            <tr key={v.id}>
              <td>{v.registrationNumber}</td>
              <td>{v.capacity}</td>
              <td>{v.driver.fullName}</td>
              <td>{v.driver.status}</td>
              <td>{v.approvalStatus}</td>
              <td>
                {v.photoUrls.map((url, i) => (
                  <a key={url} href={url} target="_blank" rel="noreferrer">
                    photo {i + 1}{" "}
                  </a>
                ))}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
