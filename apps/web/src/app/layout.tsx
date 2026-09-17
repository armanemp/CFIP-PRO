import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "CFIP-PRO",
  description: "Financial market intelligence terminal",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" dir="ltr">
      <body>{children}</body>
    </html>
  );
}
