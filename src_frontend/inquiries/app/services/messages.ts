import { Message } from "@/app/types/message";

const API_URL = 'http://localhost:5148';

interface GetMessagesParams {
  page: number;
  limit: number;
  onlyUnread?: boolean;
}

interface MessagesResponse {
  data: Message[];
  total: number;
}

export async function getMessages({ page, limit, onlyUnread }: GetMessagesParams): Promise<MessagesResponse> {
  const url = new URL(`${API_URL}/anonymous-users/messages`);
  url.searchParams.set('page', page.toString());
  url.searchParams.set('limit', limit.toString());
  if (onlyUnread) {
    url.searchParams.set('onlyUnread', 'true');
  }

  const response = await fetch(url, {
    method: 'GET',
    cache: 'no-store'
  });

  if (!response.ok) {
    throw new Error('Failed to fetch messages');
  }
  return response.json();
}

export async function toggleMessageReadStatus(id: string, isRead: boolean): Promise<void> {  
  const response = await fetch(
    `${API_URL}/messages/${id}?isRead=${isRead}`,
    {
      method: 'PUT',
      cache: 'no-store'
    }
  );

  if (!response.ok) {
    throw new Error('Failed to update message status');
  }
} 