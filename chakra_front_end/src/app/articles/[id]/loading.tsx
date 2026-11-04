import TacticalLoader from '@/components/loading/TacticalLoader'

export default function ArticleDetailLoading() {
  return (
    <TacticalLoader
      title="LOADING..."
      subtitle="正在加载文章内容..."
      mode="full"
    />
  )
}
