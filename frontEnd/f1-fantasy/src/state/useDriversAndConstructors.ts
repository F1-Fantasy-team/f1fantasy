import { useRecoilValue } from "recoil";
import { driversState, constructorsState, driversFromApiState, constructorsFromApiState } from "./atoms";

export function useDrivers() {
  return useRecoilValue(driversState);
}

export function useConstructors() {
  return useRecoilValue(constructorsState);
}

export function useDriversFromApi() {
  return useRecoilValue(driversFromApiState);
}

export function useConstructorsFromApi() {
  return useRecoilValue(constructorsFromApiState);
}
