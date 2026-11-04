import TacticalLoader from '@/components/loading/TacticalLoader'

export default function SeriesDetailLoading() {
  return (
    <TacticalLoader
      title="LOADING..."
      subtitle="正在加载专栏内容..."
      mode="full"
    />
  )
}
