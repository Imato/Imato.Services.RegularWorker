create table if not exists workers 
	(id int not null generated always as identity primary key,
	name varchar(255) not null, 
	host varchar(255) not null, 
	appName varchar(512) not null default '', 
	date timestamp with time zone not null, 
	settings text not null, 
	active bool not null, 
	executed timestamp with time zone, 
	hosts int, 
		unique (name, host, appName));