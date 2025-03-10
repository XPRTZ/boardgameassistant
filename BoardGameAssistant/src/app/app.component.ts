import { HttpClient, HttpClientModule } from '@angular/common/http';
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { ChromaClient } from "chromadb";

import { MarkdownNodeParser } from "@llamaindex/core/node-parser";
import { Document } from "@llamaindex/core/schema";
import { range } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HttpClientModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent {
  private httpClient: HttpClient;

  constructor(http: HttpClient) {
    this.httpClient = http;
  }
  
  title = 'BoardGameAssistant';

  async ngOnInit() {


    const client = new ChromaClient({ path: "http://localhost:8000" })
    client.heartbeat()
        
    if (await client.countCollections() > 0 ) {
      await client.deleteCollection({ name: "boardgame_rules" })
    }

    const collection = await client.getOrCreateCollection({
      name: "boardgame_rules",
    });

    console.log({items: await collection.count()})

//    const ruleBooks = ['wm_rulebook_v29', 'Inventions Rules Public', 'Kanban_EV_Rules_v0.99', 'Lisboa_rulebook_final', 'On_Mars_-_English_Rulebook_FINAL', 'Speakeasy Rules v16'];
//const ruleBooks = [];
const ruleBooks = ['On_Mars_-_English_Rulebook_FINAL', 'wm_rulebook_v29'];

const collectionPromises = ruleBooks.map(ruleBook => {
    this.httpClient.get(`/Rulebooks/${ruleBook}.md`, {responseType: 'text'})
        .subscribe(async text => {
          const splitter = new MarkdownNodeParser();
const nodes = splitter.getNodesFromDocuments([new Document({ text })]);
const chunks = nodes.map(node => node.text);
const ids = nodes.map(node => node.id_);
const metadatas = nodes.map(_ => ({ ruleBook}));
/*
[
{
  content: '',
  ruleBook: '',
  url: `/Rulebooks/${ruleBook}-small.md`
}
]*/

console.log({chunks, ids, metadatas})

          return collection.add({
            documents: chunks,
            ids: ids,
            metadatas: metadatas,
          });
        });      
    });   

    await Promise.all(collectionPromises);
    

    const results = await collection.query({
      queryTexts: "Which game features the professor LATIV?", // Chroma will embed this for you
      nResults: 5, // how many results to return
    });
    
    console.log(results);

    console.log({items: await collection.count()})
  }
}
