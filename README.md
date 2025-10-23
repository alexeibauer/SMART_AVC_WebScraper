# Webscraping of hotel pricing data. Built for SMART recruitment by Alex Valdes Calderon

# Dependencies

- RabbitMQ libraries for .NET
- RabbitMQ server

# Overview

This project is a web scraper designed to extract hotel pricing data from boutique hotel websites. 
It utilizes RabbitMQ for message queuing to handle scraping tasks efficiently.

Projects:

- **WebScraperConsoleApp**: Main web scraping application that processes URLs from user input and sends url as event to RabbitMQ
- **WebScraperWorker**: Worker application that listens to RabbitMQ queue, performs web scraping, and stores the extracted data.
- **WebScraperLogic**: Contains the core logic for web scraping and data extraction.
- **SMARTUtils**: Utility library for common functions used across projects.*

# Examples of URLs to scrape

	1.  https://www.booking.com/rooms/city/us/ojai.html
	2.  https://boutiquehotelhub.com/boutique-hotels/united-states/
	 