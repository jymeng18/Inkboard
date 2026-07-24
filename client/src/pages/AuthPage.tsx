import { Link } from "react-router-dom";

import AuthForm from "../components/auth/AuthForm";
import BrandPanel from "../components/auth/BrandPanel";

export default function AuthPage() {
  return (
    <div className="flex min-h-screen flex-col bg-surface font-body text-on-background">
      <main className="grid flex-1 lg:grid-cols-[2fr_3fr]">
        <BrandPanel />

        <div className="flex flex-col items-center justify-center px-6 py-14 md:px-16">
          <Link to="/" className="mb-10 flex items-center gap-3 lg:hidden">
            <img src="/pen.svg" alt="" className="h-12 rotate-[-30deg]" />
            <span className="font-display text-3xl">Inkboard</span>
          </Link>

          <AuthForm />
        </div>
      </main>
    </div>
  );
}
