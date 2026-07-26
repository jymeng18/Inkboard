import { useNavigate } from "react-router-dom";
import CanvasPreview from "@/components/landing/CanvasPreview";
import Doodles from "@/components/landing/Doodles";
import FinalCta from "@/components/landing/FinalCta";
import Footer from "@/components/landing/Footer";
import Hero from "@/components/landing/Hero";
import Navbar from "@/components/landing/Navbar";
import PartyMode from "@/components/landing/PartyMode";
import UnderTheHood from "@/components/landing/UnderTheHood";
import Ribbons from "@/components/landing/Ribbons";

/*
 * Marker colors straight from the theme tokens: coral, highlighter yellow, sky.
 * Hoisted so the identity is stable, otherwise Ribbons tears down and rebuilds
 * its WebGL context on every render.
 */
const RIBBON_COLORS = ["#ff7070", "#ffd23f", "#00c1fd"];

export default function LandingPage() {
  const navigate = useNavigate();
  const goToLogin = () => navigate("/login");

  return (
    <div
      id="top"
      className="min-h-screen bg-background font-body text-on-background"
    >
      <Navbar onStart={goToLogin} />
      <div className="pointer-events-none fixed inset-0 z-50">
        <Ribbons
          colors={RIBBON_COLORS}
          baseThickness={14}
          outlineColor="#2d2926"
          outlineWidth={4}
          offsetFactor={0.03}
          speedMultiplier={0.4}
          maxAge={400}
          enableFade={false}
        />
      </div>

      <main className="mx-auto flex max-w-360 flex-col items-center px-6 pt-14">
        <Hero onStart={goToLogin} />
        <CanvasPreview />
        <UnderTheHood />
        <PartyMode />
        <Doodles />
        <FinalCta onStart={goToLogin} />
      </main>

      <Footer />
    </div>
  );
}
