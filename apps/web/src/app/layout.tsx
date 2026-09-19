import type { Metadata } from "next";
import "./globals.css";
import { AppLocaleSync } from "@/components/app-locale-sync";

export const metadata: Metadata = {
  title: "CFIP-PRO",
  description: "Financial market intelligence terminal",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" dir="ltr">
      <body><AppLocaleSync />{children}</body>
    </html>
  );
}
