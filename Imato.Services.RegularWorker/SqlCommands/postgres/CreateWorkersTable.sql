create table if not exists workers 
	(id int not null generated always as identity primary key,
	name varchar(255) not null, 
	host varchar(255) not null, 
	appName varchar(512) not null default '', 
	date timestamp not null, 
	settings text not null, 
	active bool not null, 
	executed timestamp, 
	hosts int, 
	error text,
		unique (name, host, appName));