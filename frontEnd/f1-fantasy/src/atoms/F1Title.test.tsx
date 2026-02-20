import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { F1Title } from "./F1Title";

describe("F1Title", () => {
  it("renders text content", () => {
    render(<F1Title>Hello F1</F1Title>);
    expect(screen.getByText("Hello F1")).toBeInTheDocument();
  });

  it("uses level 1 by default", () => {
    const { container } = render(<F1Title>Title</F1Title>);
    const el = container.querySelector("h1");
    expect(el).toBeInTheDocument();
    expect(el?.textContent).toBe("Title");
  });

  it("uses specified level", () => {
    const { container } = render(<F1Title level={3}>Heading</F1Title>);
    const el = container.querySelector("h3");
    expect(el).toBeInTheDocument();
    expect(el?.textContent).toBe("Heading");
  });
});
