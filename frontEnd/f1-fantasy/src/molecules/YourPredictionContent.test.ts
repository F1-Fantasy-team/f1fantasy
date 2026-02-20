import { describe, it, expect } from "vitest";
import { getDriverName, getConstructorName } from "./YourPredictionContent";
import type { Driver } from "../types/driver";
import type { Constructor } from "../types/constructor";

describe("getDriverName", () => {
  it("returns driver name when id exists in list", () => {
    const drivers: Driver[] = [
      { id: "ver", name: "Max Verstappen" },
      { id: "nor", name: "Lando Norris" },
    ];
    expect(getDriverName("ver", drivers)).toBe("Max Verstappen");
    expect(getDriverName("nor", drivers)).toBe("Lando Norris");
  });

  it("returns id when driver not in list", () => {
    const drivers: Driver[] = [{ id: "ver", name: "Max Verstappen" }];
    expect(getDriverName("unknown", drivers)).toBe("unknown");
  });

  it("returns id when list is empty", () => {
    expect(getDriverName("lec", [])).toBe("lec");
  });
});

describe("getConstructorName", () => {
  it("returns constructor name when id exists in list", () => {
    const constructors: Constructor[] = [
      { id: "rbr", name: "Red Bull Racing" },
      { id: "fer", name: "Ferrari" },
    ];
    expect(getConstructorName("rbr", constructors)).toBe("Red Bull Racing");
    expect(getConstructorName("fer", constructors)).toBe("Ferrari");
  });

  it("returns id when constructor not in list", () => {
    const constructors: Constructor[] = [{ id: "rbr", name: "Red Bull Racing" }];
    expect(getConstructorName("mcl", constructors)).toBe("mcl");
  });

  it("returns id when list is empty", () => {
    expect(getConstructorName("mer", [])).toBe("mer");
  });
});
