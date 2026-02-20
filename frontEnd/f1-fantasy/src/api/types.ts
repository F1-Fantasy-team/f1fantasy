/**
 * API response types matching the backend (F1Fantasy) JSON shape.
 * Backend uses camelCase by default in ASP.NET Core.
 */

export interface DriverApi {
  driverId: string;
  /** Race drivers have a number; reserves may omit or have empty. */
  permanentNumber?: string;
  code: string;
  url: string;
  givenName: string;
  familyName: string;
  dateOfBirth: string;
  nationality: string;
}

export interface ConstructorApi {
  constructorId: string;
  url: string;
  name: string;
  nationality: string;
}

export interface SeasonApi {
  year: string;
  url: string;
}

export interface LocationApi {
  lat: string;
  long: string;
  locality: string;
  country: string;
}

export interface CircuitApi {
  circuitId: string;
  url: string;
  circuitName: string;
  location: LocationApi;
}

export interface RaceApi {
  season: string;
  round: string;
  url: string;
  raceName: string;
  circuit: CircuitApi;
  date: string;
  time: string;
}
