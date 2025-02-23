import { useQuery } from "@tanstack/react-query";
import { Alert } from "@/types/alert";

export function useAlerts(onlyUnread: boolean = false) {
  const { data, isLoading, refetch } = useQuery<Alert[]>({
    queryKey: ["alerts", onlyUnread],
    queryFn: async () => {
      const response = await fetch(`http://localhost:5148/alerts${onlyUnread ? '?onlyUnread=true' : ''}`);
      if (!response.ok) throw new Error("Failed to fetch alerts");
      return response.json();
    },
  });

  const changeIsRead = async (id: string, isRead: boolean) => {
    await fetch(`http://localhost:5148/alerts/${id}?isRead=${isRead}`, {
      method: 'PUT'
    });
    refetch();
  };

  return { data, isLoading, refetch, changeIsRead };
}