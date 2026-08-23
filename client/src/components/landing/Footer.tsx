import { SquarePen } from "lucide-react";

const COLUMNS = [
  {
    heading: "Documentation",
    accent: "text-primary",
    hover: "hover:text-primary",
    links: ["Backend", "Frontend", "Services", "Architecture"],
    urls: [
      "https://github.com/jymeng18/Inkboard/tree/main/docs/Backend",
      "https://github.com/jymeng18/Inkboard/tree/main/docs/Frontend",
      "https://github.com/jymeng18/Inkboard/tree/main/docs/Services",
      "https://github.com/jymeng18/Inkboard/blob/main/docs/ARCHITECTURE.md",
    ],
  },
  {
    heading: "About Creator",
    accent: "text-secondary",
    hover: "hover:text-secondary",
    links: ["GitHub", "LinkedIn", "Portfolio", "Inkboard"],
    urls: [
      "https://github.com/jymeng18",
      "https://ca.linkedin.com/in/jerry-meng18",
      "https://portfolio-jerrymeng777.vercel.app/",
      "https://github.com/jymeng18/Inkboard",
    ],
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
              {column.links.map((link, index) => (
                <li key={link}>
                  <a href={column.urls[index]} className={`transition-colors ${column.hover}`}>
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
      </div>
    </footer>
  );
}
