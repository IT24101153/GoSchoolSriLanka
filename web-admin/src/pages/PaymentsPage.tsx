import { useState } from "react";
import { useFetch } from "../api/useFetch";
import type { Payment } from "../types";

interface PaymentRow {
  payment: Payment;
  student: string;
  parent: string;
  driver: string;
  route: string;
}

const now = new Date();

export function PaymentsPage() {
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const { data, error } = useFetch<PaymentRow[]>(`/admin/payments?year=${year}&month=${month}`);

  return (
    <section>
      <h1>Parent → Driver Payments</h1>
      <label>
        Year <input id="year" type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} />
      </label>
      <label>
        Month <input id="month" type="number" min={1} max={12} value={month} onChange={(e) => setMonth(Number(e.target.value))} />
      </label>
      {error && <p role="alert">{error}</p>}
      <table>
        <thead>
          <tr>
            <th>Student</th>
            <th>Parent</th>
            <th>Driver</th>
            <th>Route</th>
            <th>Amount</th>
            <th>Method</th>
            <th>Status</th>
            <th>Slip</th>
            <th>Paid at</th>
          </tr>
        </thead>
        <tbody>
          {data?.map(({ payment: p, student, parent, driver, route }) => (
            <tr key={p.id}>
              <td>{student}</td>
              <td>{parent}</td>
              <td>{driver}</td>
              <td>{route}</td>
              <td>LKR {p.amount}</td>
              <td>{p.method}</td>
              <td>{p.status}</td>
              <td>
                {p.slipImageUrl ? (
                  <a href={p.slipImageUrl} target="_blank" rel="noreferrer">
                    view
                  </a>
                ) : (
                  "—"
                )}
              </td>
              <td>{p.paidAt ? new Date(p.paidAt).toLocaleString() : "—"}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {data?.length === 0 && <p>No payments for this month.</p>}
    </section>
  );
}
