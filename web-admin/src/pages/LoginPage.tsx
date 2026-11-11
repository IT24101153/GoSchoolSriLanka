import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { apiClient, errorMessage } from "../api/client";
import type { User } from "../types";

export function LoginPage() {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      const { data } = await apiClient.post<{ token: string; user: User }>("/auth/login", { email, password });
      if (data.user.role !== "Admin") {
        setError("This portal is for admin accounts only");
        return;
      }
      localStorage.setItem("token", data.token);
      navigate("/");
    } catch (err) {
      setError(errorMessage(err));
    }
  }

  return (
    <section>
      <h1>GoSchool Admin</h1>
      <form onSubmit={submit}>
        <label>
          Email
          <input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </label>
        <label>
          Password
          <input id="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </label>
        <button type="submit">Log in</button>
      </form>
      {error && <p role="alert">{error}</p>}
    </section>
  );
}
