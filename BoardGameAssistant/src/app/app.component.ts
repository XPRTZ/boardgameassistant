import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { ChromaClient } from "chromadb";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'BoardGameAssistant';

  async ngOnInit() {
    const client = new ChromaClient({ path: "http://localhost:8000" })
    client.heartbeat()
        
    const collection = await client.getOrCreateCollection({
      name: "boardgame_rules",
    });
        
    await collection.add({
      documents: [
          "This is a document about monopoly",
          "This is a document about uno",
      ],
      ids: ["monopoly", "uno"],
    });
    
    const results = await collection.query({
      queryTexts: "Which document can tell me more about monopoly?", // Chroma will embed this for you
      nResults: 2, // how many results to return
    });
    
    console.log(results);
  }
}
