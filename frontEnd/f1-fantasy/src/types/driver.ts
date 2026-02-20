export interface Driver {
  id: string;
  name: string;
  teamId?: string;
  /** Optional headshot/avatar URL for display in predictions */
  imageUrl?: string;
}
