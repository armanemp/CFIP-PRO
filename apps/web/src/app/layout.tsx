import type { Metadata } from "next";
import "./globals.css";
import { PlatformIdentityRuntime } from "@/components/platform-identity";

export const metadata: Metadata = {
  title: "Market Intelligence",
  description: "Configurable financial market intelligence terminal",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" dir="ltr">
      <body><PlatformIdentityRuntime />{children}</body>
    </html>
  );
}
