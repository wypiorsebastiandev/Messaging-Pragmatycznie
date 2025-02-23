"use client"

import { ApplicationTile } from "@/components/application-tile";
import { faServer, faDatabase, faTicket, faBug, faStopwatch, faComments } from "@fortawesome/free-solid-svg-icons";

export default function ApplicationsList() {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      <ApplicationTile 
        applicationName="Inquiries.Service"
        applicationBaseAddress="http://localhost:5011"
        icon={faBug}
      />
      <ApplicationTile 
        applicationName="Tickets.Service"
        applicationBaseAddress="http://localhost:5112"
        icon={faTicket}
      />
      <ApplicationTile 
        applicationName="SLA"
        applicationBaseAddress="http://localhost:5054"
        icon={faStopwatch}
      />
      <ApplicationTile 
        applicationName="Communication"
        applicationBaseAddress="http://localhost:5148"
        icon={faComments}
      />
    </div>
  );
}
