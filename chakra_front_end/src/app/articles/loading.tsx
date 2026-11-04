import { BookOpen } from 'lucide-react'

export default function ArticlesLoading() {
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12">
      <div className="mb-8">
        <div className="h-6 w-32 bg-theme-surface-hover rounded-lg animate-pulse mb-2" />
        <div className="flex items-center gap-3">
          <BookOpen className="w-7 h-7 text-theme-primary/60 animate-pulse" />
          <div className="h-8 w-64 bg-slate-200 dark:bg-slate-800 rounded-lg animate-pulse" />
        </div>
      </div>

      <div className="bg-theme-surface/90 rounded-2xl border border-theme-border/60 p-5 shadow-paper mb-8">
        <div className="flex gap-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-8 w-20 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse" />
          ))}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {Array.from({ length: 6 }).map((_, i) => (
          <div key={i} className="bg-theme-surface/90 rounded-2xl border border-theme-border/60 shadow-paper overflow-hidden">
            <div className="h-48 bg-slate-100 dark:bg-slate-800 animate-pulse" />
            <div className="p-5 space-y-3">
              <div className="h-4 w-3/4 bg-slate-200 dark:bg-slate-700 rounded animate-pulse" />
              <div className="h-3 w-full bg-slate-100 dark:bg-slate-800 rounded animate-pulse" />
              <div className="h-3 w-2/3 bg-slate-100 dark:bg-slate-800 rounded animate-pulse" />
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
