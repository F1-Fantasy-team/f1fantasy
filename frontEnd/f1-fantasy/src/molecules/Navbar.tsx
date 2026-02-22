import { useUser, UserButton } from "@clerk/clerk-react";

export default function Navbar() {
  const { isSignedIn } = useUser();

  return (
    <header className="border-b border-f1-gray bg-f1-carbon">
      <div className="container mx-auto flex h-14 max-w-full items-center justify-between px-3 sm:px-4">
        <a href="/" className="navbar-brand min-w-0 truncate text-base font-bold tracking-tight text-f1-white sm:text-lg">
          F1 Fantasy
        </a>
        {isSignedIn && (
          <UserButton
            afterSignOutUrl="/"
            appearance={{
              variables: {
                colorPrimary: "#e10600",
              },
            }}
          />
        )}
      </div>
    </header>
  );
}
