import type { MetadataRoute } from 'next'
import { getSiteConfig } from '@/lib/site-config'

export const dynamic = 'force-dynamic'

export default async function robots(): Promise<MetadataRoute.Robots> {
  const config = await getSiteConfig()

  return {
    rules: [
      {
        userAgent: '*',
        allow: '/',
        disallow: ['/api/', '/_next/'],
      },
    ],
    sitemap: `${config.url}/sitemap.xml`,
  }
}
