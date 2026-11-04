import { Layers } from 'lucide-react'

export default function SeriesLoading() {
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12">
      <div className="mb-10">
        <div className="h-6 w-24 bg-theme-surface-hover rounded-lg animate-pulse mb-2" />
        <div className="flex items-center gap-3">
          <Layers className="w-7 h-7 text-theme-primary/60 animate-pulse" />
          <div className="h-8 w-72 bg-slate-200 dark:bg-slate-800 rounded-lg animate-pulse" />
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="bg-theme-surface/90 rounded-3xl border border-theme-border/60 shadow-paper overflow-hidden">
            <div className="h-56 bg-slate-100 dark:bg-slate-800 animate-pulse" />
            <div className="p-6 space-y-4">
              <div className="h-5 w-3/4 bg-slate-200 dark:bg-slate-700 rounded animate-pulse" />
              <div className="h-3 w-full bg-slate-100 dark:bg-slate-800 rounded animate-pulse" />
              <div className="p-3.5 bg-slate-50 dark:bg-slate-800/60 rounded-2xl border border-slate-100 dark:border-slate-700 space-y-2">
                {Array.from({ length: 3 }).map((_, j) => (
                  <div key={j} className="h-3 bg-slate-200 dark:bg-slate-700 rounded animate-pulse" />
                ))}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
