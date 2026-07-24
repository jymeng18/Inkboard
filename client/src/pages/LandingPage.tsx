import { useNavigate } from 'react-router-dom'
import CanvasPreview from '../components/landing/CanvasPreview'
import Doodles from '../components/landing/Doodles'
import FinalCta from '../components/landing/FinalCta'
import Footer from '../components/landing/Footer'
import Hero from '../components/landing/Hero'
import Navbar from '../components/landing/Navbar'
import PartyMode from '../components/landing/PartyMode'
import UnderTheHood from '../components/landing/UnderTheHood'

export default function LandingPage() {
  const navigate = useNavigate()
  const goToLogin = () => navigate('/login')

  return (
    <div id="top" className="min-h-screen bg-background font-body text-on-background">
      <Navbar onStart={goToLogin} />

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
  )
}
