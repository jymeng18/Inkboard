import { Globe, Share2, SquarePen } from "lucide-react";

const COLUMNS = [
  {
    heading: "Product",
    accent: "text-primary",
    hover: "hover:text-primary",
    links: ["Infinity Canvas", "AI Sketcher", "Sync Engine", "Pricing"],
  },
  {
    heading: "Support",
    accent: "text-secondary",
    hover: "hover:text-secondary",
    links: ["GitHub", "LinkedIn", "Creator", "Documentation"],
  },
];

export default function Footer() {
  return (
    <footer className="bg-on-background pt-16 pb-8 text-white">
      <div className="mx-auto mb-16 grid max-w-5xl grid-cols-1 gap-10 px-6 md:grid-cols-4">
        <div className="md:col-span-2">
          <div className="mb-5 flex items-center gap-2">
            <SquarePen className="size-6 text-primary" aria-hidden />
            <span className="font-display text-3xl">Inkboard</span>
          </div>
          <p className="max-w-sm font-body text-sm text-white/60">
            The real-time collaborative creative canvas for teams who love
            sharing.
          </p>
        </div>

        {COLUMNS.map((column) => (
          <div key={column.heading}>
            <h3
              className={`mb-6 font-label text-sm font-bold tracking-widest uppercase ${column.accent}`}
            >
              {column.heading}
            </h3>
            <ul className="space-y-3 font-body text-sm text-white/80">
              {column.links.map((link) => (
                <li key={link}>
                  <a href="#" className={`transition-colors ${column.hover}`}>
                    {link}
                  </a>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>

      <div className="mx-auto flex max-w-5xl flex-col items-center justify-between gap-6 border-t border-white/10 px-6 pt-8 md:flex-row">
        <p className="font-body text-sm text-white/40">
          © {new Date().getFullYear()} Inkboard Inc. All rights reserved.
        </p>
        <div className="flex gap-8">
          <a
            href="#"
            aria-label="Share"
            className="text-white/40 transition-colors hover:text-white"
          >
            <Share2 className="size-5" aria-hidden />
          </a>
          <a
            href="#"
            aria-label="Website"
            className="text-white/40 transition-colors hover:text-white"
          >
            <Globe className="size-5" aria-hidden />
          </a>
        </div>
      </div>
    </footer>
  );
}
