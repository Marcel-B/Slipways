/**
 * Many imported places are only called "Slipanlage" or "Marina", so the town tells them apart. The API builds the
 * same title for search engines in SeoEndpoints.PlaceTitle.
 */
export function placeTitle(name: string, city: string | null): string {
  return city && !name.toLocaleLowerCase('de').includes(city.toLocaleLowerCase('de')) ? `${name} in ${city}` : name
}
