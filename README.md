# GaGSemanticMap
.NET 8 Blazor Application using a chat bot based on Semantic Kernels for finding Podcast Episodes (Currently Configured: "Geschichten aus der Geschichte") semantically related to user input, getting more information about 
certain topics and maintaining a listening queue. The Main Project is "GAGSemanticMap". "CreateDataEmbeddings" was used to create the initial episode embeddings and could be used to adapt the project to another podcast. This project was created for the "Hack-together" event from Microsoft, conducted for .NET 8 release.

## How it works
Using a semantic kernel approach, the program decides, what the users intent is based on his inputs. It then decides to either:
- Find episodes relating to the user input (based on embeddings vectors)
- Get more in depth information about a certain topic
- Add episodes to listening queue
- Ask user to provide more information if none of the above fits

The semantic kernel approach is then used for each of these pipelines. Each episode will be displayed with a link.

## Instructions
Add MODEL, ENDPOINT, KEY, TYPE(="azure") and EMBEDDING to .env file to start the application and ensure connection with Azure OpenAI client. Run the program and use the chatbox to ask questions. 
See also Flyout for more detailed instructions about questions.

Questions could for example look something like this:
- Give me episodes about Africa.
- Can you give me episodes about the 19th Century?
- Can you tell me more about the great smog of London?
- Can you add Episode GAG234 to my listening queue?
- Add all items to listening queue

For a demo, see here: https://www.youtube.com/watch?v=hyoEp0OfEq4

Very rarely, there might be an issue with displaying the answer. In this case, restart the application and reformulate the question to get a different result and a proper display.

### Adaptation for different podcasts
If a user would wish to configure another podcasts, they need to append to the currently used GAG_181123.csv. The user should create an .xlsx of shape: 

Point(not used);Name;Link;Description;Embedding. The embeddings can be left blank first.

Then the user runs the project "CreateDataEmbeddings" to create the embeddings based on the episode descriptions of the given podcasts. Embedding Vectors will be added to the .xlsx file. 

The file can than be converted to .csv and appended to the current file, which acts as a substitute for a database backing. 

## Outlook
The following things would be cool to implement to make the application more useful:
- A database backing (instead of a CSV file), that could also be persisted per user to keep the listening queue (or enable sending of the listening queue per email)
- With a database backing, the proper approach for memory store could be used for the embeddings, and then query the podcast directly from memory.
- Make a drag and drop field where a csv without embeddings could be dropped, embeddings calculated and then the podcast episodes would be available for query. This could also work for mixing multiple podcasts, which might be cool.
  
