/**
 * Reads the payload of a JWT.
 *
 * `atob` alone is not enough: it returns one character per byte, so a UTF-8 name such as
 * "Jelena Markovic" with diacritics comes back mangled. The bytes are therefore decoded as
 * UTF-8. The payload also uses base64url, which `atob` does not accept without translation.
 */
export interface JwtClaims {
  sub?: string;
  nameid?: string;
  name?: string;
  unique_name?: string;
  username?: string;
  role?: string;
  /** Claims issued under their full schema URI are read by their key. */
  readonly [claim: string]: string | undefined;
}

export function decodeJwtPayload<T = JwtClaims>(token: string | null | undefined): T | null {
  const segment = token?.split('.')[1];
  if (!segment) {
    return null;
  }

  try {
    const base64 = segment.replace(/-/g, '+').replace(/_/g, '/')
      .padEnd(Math.ceil(segment.length / 4) * 4, '=');
    const bytes = Uint8Array.from(atob(base64), character => character.charCodeAt(0));
    return JSON.parse(new TextDecoder('utf-8').decode(bytes)) as T;
  } catch {
    return null;
  }
}
