export interface Driver {
  id: string;
  name: string;
  teamId?: string;
  /** Optional headshot/avatar URL for display in predictions */
  imageUrl?: string;
  /** Wikipedia page URL from API; used to fetch main image via Wikipedia REST API */
  wikipediaUrl?: string;
}
