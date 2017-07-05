import { getUriQueryParameter } from './browser';

export type Feature =
    "Placeholder"   // TODO: replace this with our first feature flag

export const isEnabled = (feature:Feature):boolean => getUriQueryParameter(`enable${feature}`) === "1"